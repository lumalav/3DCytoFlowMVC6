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
        [Display(Name = "Completion Date")]
        public DateTime? CompletionDate { get; set; }
        [Display(Name = "Number of jobs to use")]
        [Range(1, 64, ErrorMessage = "The number of jobs to use must be between 1 and 64")]
        public Int32 JobNumber { get; set; }
        [Display(Name = "Number of points to generate")]
        [Range(10.0, 10000000.0, ErrorMessage = "The number of points to be generated must be between 10 and 1 000 000")]
        public Int32 PointNumber { get; set; }
    }
}
