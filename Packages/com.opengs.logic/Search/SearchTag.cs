using System.Collections.Generic;
using System.Linq;

namespace OpenGSCore
{
    public class SearchTag
    {
        private readonly HashSet<string> tags = new();

        public SearchTag()
        {
        }

        public SearchTag(IEnumerable<string> initialTags)
        {
            if (initialTags == null)
            {
                return;
            }

            foreach (var tag in initialTags.Where(tag => !string.IsNullOrWhiteSpace(tag)))
            {
                tags.Add(tag.Trim());
            }
        }

        public IReadOnlyCollection<string> Tags => tags;

        public void SetPlayerTag() => tags.Add("Player");
        public void SetBotTag() => tags.Add("Bot");
        public void SetEnemyTag() => tags.Add("Enemy");
        public bool Has(string tag) => !string.IsNullOrWhiteSpace(tag) && tags.Contains(tag);
    }
}
