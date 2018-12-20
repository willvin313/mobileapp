using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Models;
using Accord.Math.Optimization.Losses;
using Accord.MachineLearning.DecisionTrees;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Diagnostics;

namespace Toggl.Foundation.Suggestions
{
    public sealed class RandomForestSuggestionProvider : ISuggestionProvider
    {
        private readonly ITogglDataSource dataSource;
        private readonly ITimeService timeService;
        private readonly IStopwatchProvider stopwatchProvider;
        private readonly int maxNumberOfSuggestions;

        private readonly int maxNumberOfTimeEntriesForTraining = 1000;
        private readonly int minNumberOfTimeEntriesForTraining = 100;
        private readonly int forestNumberOfTrees = 5;
        private readonly int forestJoin = 100;

        public RandomForestSuggestionProvider(
            IStopwatchProvider stopwatchProvider,
            ITogglDataSource dataSource,
            ITimeService timeService, 
            int maxNumberOfSuggestions)
        {
            Ensure.Argument.IsNotNull(stopwatchProvider, nameof(stopwatchProvider));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsInClosedRange(maxNumberOfSuggestions, 1, 9, nameof(maxNumberOfSuggestions));

            this.stopwatchProvider = stopwatchProvider;
            this.dataSource = dataSource;
            this.timeService = timeService;
            this.maxNumberOfSuggestions = maxNumberOfSuggestions;
        }

        public IObservable<Suggestion> GetSuggestions()
            => dataSource.TimeEntries
                     .GetAll()
                     .Select(predictUsingRandomForestClassifier)
                     .SelectMany(toSuggestions);

        private IEnumerable<Suggestion> toSuggestions(IEnumerable<(IDatabaseTimeEntry id, float score)> timeEntriesAndScores)
            => timeEntriesAndScores.Select(timeEntriesAndScore => new Suggestion(timeEntriesAndScore.id, timeEntriesAndScore.score, SuggestionProviderType.RandomForest));


        private IEnumerable<(IDatabaseTimeEntry, float)> predictUsingRandomForestClassifier(IEnumerable<IDatabaseTimeEntry> timeEntriesForPrediction)
        {
            var timeEntries = timeEntriesForPrediction
                .Where(te => te.ProjectId.HasValue)
                .Take(maxNumberOfTimeEntriesForTraining)
                .ToList();

            if (timeEntries.Count() >= minNumberOfTimeEntriesForTraining)
            {
                return predictUsing2Steps(timeEntries);
            }

            timeEntries = timeEntriesForPrediction
                .Take(maxNumberOfTimeEntriesForTraining)
                .ToList();

            if (timeEntries.Count() >= minNumberOfTimeEntriesForTraining)
            {
                return predictUsing1Step(timeEntries);
            }

            return new List<(IDatabaseTimeEntry, float)>();
        }

        private IEnumerable<(IDatabaseTimeEntry, float)> predictUsing2Steps(List<IDatabaseTimeEntry> timeEntries)
        {
            var projectPredictionStopWatch = stopwatchProvider.Get(MeasuredOperation.RandomForest2StepsProjectPrediction);
            projectPredictionStopWatch.Start();
            var predictedProjectTuple = predictProjectID(timeEntries); //Step 1 Predict Projects
            projectPredictionStopWatch.Stop();
            
            var timeEntriesFromProject = timeEntries.Where(te => te.ProjectId == predictedProjectTuple.id).ToList();

            var timeEntryPredictionStopWatch = stopwatchProvider.Get(MeasuredOperation.RandomForest2StepsTimeEntryPrediction);
            timeEntryPredictionStopWatch.Start();
            var predictedTimeEntryTuple = predictTimeEntryID(timeEntriesFromProject); //Step 2 Predict the TimeEntry
            timeEntryPredictionStopWatch.Stop();

            var avgScore = (predictedProjectTuple.score + predictedTimeEntryTuple.score) / 2;

            return new[] { (timeEntries.FirstOrDefault(te => te.Id == predictedTimeEntryTuple.id), avgScore) };
        }

        private IEnumerable<(IDatabaseTimeEntry, float)> predictUsing1Step(List<IDatabaseTimeEntry> timeEntries)
        {
            var timeEntryPredictionStopWatch = stopwatchProvider.Get(MeasuredOperation.RandomForest1StepTimeEntryPrediction);
            timeEntryPredictionStopWatch.Start();
            var predictedTimeEntryTuple = predictTimeEntryID(timeEntries);
            timeEntryPredictionStopWatch.Stop();

            return new[] { (timeEntries.FirstOrDefault(te => te.Id == predictedTimeEntryTuple.id), predictedTimeEntryTuple.score) };
        }

        private double[] toFeatures(DateTimeOffset dateTimeOffset)
        {
            var start = dateTimeOffset;
            var dayOfWeek = (int)start.DayOfWeek;
            var startHour = TimeZoneInfo.ConvertTime(start, TimeZoneInfo.Local).Hour;

            var dayOfWeekNormalized = (double)dayOfWeek / 6;
            var isWeekend = dayOfWeek == 0 || dayOfWeek == 6 ? 1 : 0;
            var isBusinessHours = startHour > 8 && startHour < 18 ? 1 : 0;
            var startHourNormalized = (double)startHour / 23;

            var features = new[]
            {
                dayOfWeekNormalized,
                isWeekend,
                isBusinessHours,
                startHourNormalized
            };

            return features;
        }

        private double[][] featureRemovingUnchangingValues(double[][] input)
        {
            var features = input;
            if (features.Length == 0)
            {
                return features;
            }

            if (features[0].Length == 0)
            {
                return features;
            }

            int rowLength = features.Length;
            int colLength = features[0].Length;

            bool[] indexesWithSameValues = Enumerable.Repeat(true, colLength).ToArray();

            double[] firstRow = features[0];

            for (int i = 1; i < rowLength; i++)
            {
                for (int j = 0; j < colLength; j++)
                {
                    if (System.Math.Abs(features[i][j] - firstRow[j]) > 0)
                    {
                        indexesWithSameValues[j] = false;
                    }
                }
            }

            for (int i = 0; i < rowLength; i++)
            {
                var newRow = new List<Double>();
                for (int j = 0; j < colLength; j++)
                {
                    if (!indexesWithSameValues[j])
                    {
                        newRow.Add(features[i][j]);
                    }
                }
                features[i] = newRow.ToArray();
            }

            return features;
        }
        private (int id, float score) predict(double[][] trainingInputs, int[] trainingOutputs, double[][] inputToPredict)
        {
            var teacher = new RandomForestLearning()
            {
                NumberOfTrees = forestNumberOfTrees,
                Join = forestJoin
            };
            
            var forest = teacher.Learn(trainingInputs, trainingOutputs);

            int[] predicted = forest.Decide(inputToPredict);

            double error = new ZeroOneLoss(trainingOutputs).Loss(forest.Decide(trainingInputs));
            float score = 1 - (float)error;

            return (predicted[0], score);
        }

        private double[][] prepareInputsFor(IList<IDatabaseTimeEntry> timeEntries)
            => timeEntries.Select(te => toFeatures(te.Start)).ToArray();

        private int[] prepareProjectOutputsFor(IList<IDatabaseTimeEntry> timeEntries)
            => timeEntries.Select(te => (int)te.ProjectId.GetValueOrDefault()).ToArray();

        private int[] prepareTimeEntryOutputsFor(IList<IDatabaseTimeEntry> timeEntries)
            => timeEntries.Select(te => (int)te.Id).ToArray();

        // This method does to 2 things
        // 1. Returns a list of all unique values of the `outputs` parameter
        // 2. Changes the `outputs` parameter values to be the same as the index of that value in the returned list.
        // This is needed to reduce complexity of the data for the random forest algorithm so that it runs faster.
        // Instead of using long values of the IDs it will use smaller int basd on the index.
        private int[] uniqueOutputsFor(int[] outputs)
        {
            int[] uniqueOutputs = outputs.Distinct().ToArray();

            for (int i = 0; i < outputs.Length; i++)
            {
                outputs[i] = Array.IndexOf(uniqueOutputs, outputs[i]);
            }

            return uniqueOutputs;
        }

        private (int id, float score) predictProjectID(IEnumerable<IDatabaseTimeEntry> timeEntriesForPrediction)
        {
            var timeEntries = timeEntriesForPrediction.ToList();

            double[][] inputs = featureRemovingUnchangingValues(prepareInputsFor(timeEntries));
            int[] outputs = prepareProjectOutputsFor(timeEntries);
            int[] uniqueOutputs = uniqueOutputsFor(outputs); // This also updates the outputs to have values same as the index in the uniqueOutputs

            var inputToPredict = new double[][] { toFeatures(timeService.CurrentDateTime) };
            var prediction = predict(inputs, outputs, inputToPredict);

            return (uniqueOutputs[prediction.id], prediction.score);
        }

        private (int id, float score) predictTimeEntryID(IEnumerable<IDatabaseTimeEntry> timeEntriesForPrediction)
        {
            var timeEntries = timeEntriesForPrediction.ToList();

            double[][] inputs = featureRemovingUnchangingValues(prepareInputsFor(timeEntries));
            int[] outputs = prepareTimeEntryOutputsFor(timeEntries);
            int[] uniqueOutputs = uniqueOutputsFor(outputs); // This also updates the outputs to have values same as the index in the uniqueOutputs

            var inputToPredict = new double[][] { toFeatures(timeService.CurrentDateTime) };
            var prediction = predict(inputs, outputs, inputToPredict);

            return (uniqueOutputs[prediction.id], prediction.score);
        }
    }
}
