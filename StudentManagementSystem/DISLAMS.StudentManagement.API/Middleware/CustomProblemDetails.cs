using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace DISLAMS.StudentManagement.API.Middleware
{
    public class CustomProblemDetails : ProblemDetails
    {
        public IDictionary<string, string[]> Errors { get; set; } = new Dictionary<string, string[]>();
    }
}