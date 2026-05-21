using System.Collections.Generic;

namespace WSModTags
{
    public class WorkshopTagValidationResult
    {
        public WorkshopTagValidationResult(List<string> validTags, List<string> errors)
        {
            ValidTags = validTags ?? new List<string>();
            Errors = errors ?? new List<string>();
        }

        public List<string> ValidTags { get; private set; }

        public List<string> Errors { get; private set; }

        public bool IsValid
        {
            get { return Errors.Count == 0; }
        }
    }
}
