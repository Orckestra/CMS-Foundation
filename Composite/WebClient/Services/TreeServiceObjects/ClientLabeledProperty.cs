﻿
using Composite.Elements;
namespace Composite.WebClient.Services.TreeServiceObjects
{
	public class ClientLabeledProperty
	{
        public ClientLabeledProperty()
        { }

        public ClientLabeledProperty(LabeledProperty labeledProperty)
        {
            this.Name = labeledProperty.Name;
            this.Label = labeledProperty.Label;
            this.Value = labeledProperty.Value;
        }


        /// <summary>
        /// The name of the property. The name is constant across cultures and is intended as an id other systems can use.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The label the user should see.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// The value of the property
        /// </summary>
        public string Value { get; set; }
    }
}
