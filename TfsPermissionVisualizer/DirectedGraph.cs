using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace TfsPermissionVisualizer
{
    public class DirectedGraph
    {
        private static XNamespace dgmlNamespace = "http://schemas.microsoft.com/vs/2009/dgml";
        public XElement nodes;
        public XElement links;

        private XElement categories;
        private readonly HashSet<string> categorieSet = new HashSet<string>();

        private XElement styles;
        private XDocument dgmlGraph;

        public DirectedGraph()
        {
            this.dgmlGraph = new XDocument(
                new XElement(DirectedGraph.dgmlNamespace + "DirectedGraph",
                             new XAttribute("GraphDirection", "TopToBottom"),
                             new XAttribute("Layout", "Sugiyama"),
                             new XElement(DirectedGraph.dgmlNamespace + "Nodes"),
                             new XElement(DirectedGraph.dgmlNamespace + "Links"),
                             new XElement(DirectedGraph.dgmlNamespace + "Categories"),
                             new XElement(DirectedGraph.dgmlNamespace + "Properties"),
                             new XElement(DirectedGraph.dgmlNamespace + "Styles")));

            this.nodes = this.dgmlGraph.Root?.Element(DirectedGraph.dgmlNamespace + "Nodes");
            this.links = this.dgmlGraph.Root?.Element(DirectedGraph.dgmlNamespace + "Links");
            this.categories = this.dgmlGraph.Root?.Element(DirectedGraph.dgmlNamespace + "Categories");
            this.styles = this.dgmlGraph.Root?.Element(DirectedGraph.dgmlNamespace + "Styles");
        }

        public XDocument ProduceGraph()
        {
            string[] goodBackgroundColors = new string[] { "#FF008040", "#FF0080C0", "#FF000080", "#FF800080", "#FF7D393D", "#FF354F79", "#FF004D97", "#FF800040", "#FF008080", "#FF000080", "#FF804000", "#FF800000", "#FF400080", "#FF0080C0", "#FF00C0A0", "#FFC04000" };
            int colorCounter = 0;
            foreach (string nodeCategory in this.categorieSet)
            {
                string nodeCategoryString = nodeCategory.ToString();
                this.AddCategory(nodeCategoryString, nodeCategoryString);

                XElement style = this.AddStyle(nodeCategory);
                this.AddStyleCondition(style, "HasCategory('" + nodeCategory + "')");
                string color = goodBackgroundColors[colorCounter++ % goodBackgroundColors.Length];
                this.AddStyleSetterProperty(style, "Background", color);
            }

            return this.dgmlGraph;
        }

        public void AddNode(string id, string label, string category, string icon = null, string group = null, params Tuple<string, string>[] properties)
        {
            XElement node = new XElement(DirectedGraph.dgmlNamespace + "Node",
                new XAttribute("Id", id),
                new XAttribute("Label", label));

            if (!string.IsNullOrEmpty(category)) node.Add(new XAttribute("Category", category));
            this.AddCategory(category, category);
            if (!string.IsNullOrEmpty(icon)) node.Add(new XAttribute("Icon", icon));
            if (!string.IsNullOrEmpty(group)) node.Add(new XAttribute("Group", group));

            if (properties != null)
            {
                foreach (Tuple<string, string> property in properties)
                {
                    node.Add(new XAttribute(property.Item1, property.Item2));
                }
            }
            this.nodes.Add(node);
        }

        public void AddLink(string sourceNodeId, string targetNodeId, string category = null)
        {
            XElement link = new XElement(DirectedGraph.dgmlNamespace + "Link",
                new XAttribute("Source", sourceNodeId),
                new XAttribute("Target", targetNodeId));

            if (!string.IsNullOrEmpty(category)) link.Add(new XAttribute("Category", category));

            this.links.Add(link);
        }

        public void AddCategory(string id, string label, string canBeDataDriven = null, string canLinkedNodesBeDataDriven = null, string incomingActionLabel = null, string isContainment = null, string outgoingActionLabel = null)
        {
            if (this.categorieSet.Contains(label))
                return;
            this.categorieSet.Add(label);

            XElement category = new XElement(DirectedGraph.dgmlNamespace + "Category",
                new XAttribute("Id", id),
                new XAttribute("Label", label));

            if (!string.IsNullOrEmpty(canBeDataDriven)) category.Add(new XAttribute("CanBeDataDriven", canBeDataDriven));
            if (!string.IsNullOrEmpty(canLinkedNodesBeDataDriven)) category.Add(new XAttribute("CanLinkedNodesBeDataDriven", canLinkedNodesBeDataDriven));
            if (!string.IsNullOrEmpty(incomingActionLabel)) category.Add(new XAttribute("IncomingActionLabel", incomingActionLabel));
            if (!string.IsNullOrEmpty(isContainment)) category.Add(new XAttribute("IsContainment", isContainment));
            if (!string.IsNullOrEmpty(outgoingActionLabel)) category.Add(new XAttribute("OutgoingActionLabel", outgoingActionLabel));

            this.categories.Add(category);
        }

        public XElement AddStyle(string groupLabel, string targetType = "Node", string valueLabel = "Has category")
        {
            XElement style = new XElement(DirectedGraph.dgmlNamespace + "Style",
                new XAttribute("TargetType", targetType),
                new XAttribute("GroupLabel", groupLabel),
                new XAttribute("ValueLabel", valueLabel));

            this.styles.Add(style);
            return style;
        }

        public void AddStyleCondition(XElement style, string expression)
        {
            style.Add(new XElement(DirectedGraph.dgmlNamespace + "Condition",
                new XAttribute("Expression", expression)));
        }

        public void AddStyleSetterProperty(XElement style, string propertyName, string propertyValue)
        {
            style.Add(new XElement(DirectedGraph.dgmlNamespace + "Setter",
                new XAttribute("Property", propertyName),
                new XAttribute("Value", propertyValue)));
        }
    }
}