using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Toggl.Multivac.Extensions;
using static Toggl.Foundation.MvvmCross.ViewModels.Pomodoro.PomodoroWorkflowItemType;

namespace Toggl.Foundation.MvvmCross.ViewModels.Pomodoro
{
    public class PomodoroConfiguration
    {
        private const string rootName = "data";
        private const string workflowXmlName = "workflow";
        private const string nameXmlAttribute = "name";
        private const string idXmlAttribute = "id";
        private const string typeXmlAttribute = "type";
        private const string itemsXmlName = "items";
        private const string itemXmlName = "item";
        private const string durationXmlAttribute = "duration";
        private const string workflowReferenceAttribute = "workflow";

        private const string defaultWorkflowGuid = "6cc3cf0b-6db0-4f42-b012-b0d3bbd9984d";
        private const string defaultWorkflowName = "Default Pomodoro";

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
                new XAttribute(durationXmlAttribute, (int)item.Duration.TotalSeconds));

            if (!string.IsNullOrEmpty(item.WorkflowReference))
                itemXml.Add(new XAttribute(workflowReferenceAttribute, item.WorkflowReference));

            return itemXml;
        }

        private static PomodoroWorkflow convertToWorkflow(XElement xmlElement)
        {
            var id = xmlElement.Attribute(idXmlAttribute).Value;
            var type = xmlElement.Attribute(typeXmlAttribute).Value.ToEnumValue<PomodoroWorkflowType>();
            var name = xmlElement.Attribute(nameXmlAttribute).Value;
            var elements = xmlElement.Elements(itemsXmlName).Select(convertToWorkflowEntry);

            return new PomodoroWorkflow(id, type, name, elements);
        }

        private static PomodoroWorkflowItem convertToWorkflowEntry(XElement xmlElement)
        {
            var type = xmlElement.Attribute(typeXmlAttribute).Value.ToEnumValue<PomodoroWorkflowItemType>();

            var durationInMinutes = int.Parse(xmlElement.Attribute(durationXmlAttribute).Value);
            var duration = TimeSpan.FromMinutes(durationInMinutes);
            var workflowReference = xmlElement.Attribute(workflowReferenceAttribute)?.Value;

            return new PomodoroWorkflowItem(type, duration, workflowReference);
        }

        public static PomodoroConfiguration Default
        {
            get
            {
                var items = new PomodoroWorkflowItem[]
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

                var workflow = new PomodoroWorkflow(
                    defaultWorkflowGuid,
                    PomodoroWorkflowType.System,
                    defaultWorkflowName,
                    items);

                return new PomodoroConfiguration()
                {
                    Workflows = new List<PomodoroWorkflow> { workflow }
                };
            }
        }
    }
}
