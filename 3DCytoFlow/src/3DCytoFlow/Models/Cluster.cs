//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Newtonsoft.Json;

namespace _3DCytoFlow
{
    using System;
    using System.Collections.Generic;

    public partial class Cluster
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Depth { get; set; }

        [JsonIgnore] 
        public virtual Analysis Analysis { get; set; }
    }
}
