using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Toggl.Multivac.Extensions;
using static Toggl.Foundation.Models.Pomodoro.PomodoroWorkflowItemType;

namespace Toggl.Foundation.Models.Pomodoro
{
    public class PomodoroConfiguration
    {
        private const string rootName = "data";
        private const string workflowXmlName = "workflow";
        private const string nameXmlAttribute = "name";
        private const string idXmlAttribute = "id";
        private const string typeXmlAttribute = "type";
        private const string itemXmlName = "item";
        private const string durationXmlAttribute = "duration";
        private const string workflowReferenceAttribute = "workflow";

        private const string classicPomodoroWorkflowGuid = "6cc3cf0b-6db0-4f42-b012-b0d3bbd9984d";
        private const string classicPomodoroWorkflowName = "Classic Pomodoro";

        private const string simplifiedPomodoroWorkflowGuid = "b5fed447-9688-475f-932f-a5a3d3fbdfc7";
        private const string simplifiedPomodoroWorkflowName = "Simplified Pomodoro";

        public IReadOnlyList<PomodoroWorkflow> Workflows { get; private set; }

        private PomodoroConfiguration() { }

        public static PomodoroConfiguration FromXml(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                return null;

            var root = XElement.Parse(xml);

            var workflowsXml = root.Elements(workflowXmlName);

            return new PomodoroConfiguration()
            {
                Workflows = workflowsXml.Select(convertToWorkflow).ToList()
            };
        }

        public string ToXml()
        {
            var root = new XElement(rootName);
            Workflows.Select(convertToXml).ForEach(root.Add);
            return root.ToString();
        }

        private static XElement convertToXml(PomodoroWorkflow workflow)
        {
            var workflowXml = new XElement(workflowXmlName,
                new XAttribute(idXmlAttribute, workflow.Id),
                new XAttribute(typeXmlAttribute, workflow.Type),
                new XAttribute(nameXmlAttribute, workflow.Name));

            workflow.Items
                .Select(convertToXml)
                .ForEach(workflowXml.Add);

            return workflowXml;
        }

        private static XElement convertToXml(PomodoroWorkflowItem item)
        {
            var itemXml = new XElement(itemXmlName,
                new XAttribute(typeXmlAttribute, item.Type),
                new XAttribute(durationXmlAttribute, (int)item.Duration.TotalMinutes));

            if (!string.IsNullOrEmpty(item.WorkflowReference))
                itemXml.Add(new XAttribute(workflowReferenceAttribute, item.WorkflowReference));

            return itemXml;
        }

        private static PomodoroWorkflow convertToWorkflow(XElement xmlElement)
        {
            var id = xmlElement.Attribute(idXmlAttribute).Value;
            var type = xmlElement.Attribute(typeXmlAttribute).Value.ToEnumValue<PomodoroWorkflowType>();
            var name = xmlElement.Attribute(nameXmlAttribute).Value;
            var elements = xmlElement.Elements(itemXmlName).Select(convertToWorkflowItem);

            return new PomodoroWorkflow(id, type, name, elements);
        }

        private static PomodoroWorkflowItem convertToWorkflowItem(XElement xmlElement)
        {
            var type = xmlElement.Attribute(typeXmlAttribute).Value.ToEnumValue<PomodoroWorkflowItemType>();

            var duration = int.Parse(xmlElement.Attribute(durationXmlAttribute).Value);
            var workflowReference = xmlElement.Attribute(workflowReferenceAttribute)?.Value;

            return new PomodoroWorkflowItem(type, duration, workflowReference);
        }

        public static PomodoroConfiguration Default
        {
            get
            {
                var classicPomodoroItems = new PomodoroWorkflowItem[]
                {
                    new PomodoroWorkflowItem(Work, 25),
                    new PomodoroWorkflowItem(Rest, 5),
                    new PomodoroWorkflowItem(Work, 25),
                    new PomodoroWorkflowItem(Rest, 5),
                    new PomodoroWorkflowItem(Work, 25),
                    new PomodoroWorkflowItem(Rest, 5),
                    new PomodoroWorkflowItem(Work, 25),
                    new PomodoroWorkflowItem(Rest, 30)
                };

                var classicPomodoroWorkflow = new PomodoroWorkflow(
                    classicPomodoroWorkflowGuid,
                    PomodoroWorkflowType.System,
                    classicPomodoroWorkflowName,
                    classicPomodoroItems);

                var simplifiedPomodoroItems = new PomodoroWorkflowItem[]
                {
                    new PomodoroWorkflowItem(Work, 25),
                    new PomodoroWorkflowItem(Rest, 5)
                };

                var simplifiedPomodoroWorkflow = new PomodoroWorkflow(
                   simplifiedPomodoroWorkflowGuid,
                   PomodoroWorkflowType.System,
                   simplifiedPomodoroWorkflowName,
                   simplifiedPomodoroItems);

                return new PomodoroConfiguration()
                {
                    Workflows = new List<PomodoroWorkflow>
                    {
                        classicPomodoroWorkflow,
                        simplifiedPomodoroWorkflow
                    }
                };
            }
        }
    }
}
