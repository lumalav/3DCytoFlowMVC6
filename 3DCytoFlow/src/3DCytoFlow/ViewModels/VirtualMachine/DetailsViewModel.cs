using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace _3DCytoFlow.ViewModels.VirtualMachine
{
    public class DetailsViewModel
    {
        public string id { get; set; }
        [Display(Name = "Name")]
        public string name { get; set; }
    }
}
