using System.Collections.Generic;

namespace ProjectShift.Stage
{
    /// <summary>Pure stage select paging rules.</summary>
    public static class StagePages
    {
        /// <summary>
        /// Fills the page lists. A page starts at stage 0, at every non-empty chapter title and every
        /// <paramref name="pageSize"/> stages inside a chapter (that page keeps the chapter's title).
        /// pageChapters is the 0-based chapter index of each page.
        /// </summary>
        public static void Build(string[] chapterTitles, int pageSize, List<int> pageStarts, List<string> pageTitles, List<int> pageChapters)
        {
            pageStarts.Clear();
            pageTitles.Clear();
            pageChapters.Clear();
            string title = string.Empty;
            int chapter = 0;
            for (int i = 0; i < chapterTitles.Length; i++)
            {
                bool newChapter = !string.IsNullOrEmpty(chapterTitles[i]);
                if (newChapter)
                {
                    title = chapterTitles[i];
                    if (i > 0)
                    {
                        chapter++;
                    }
                }

                if (i == 0 || newChapter || i - pageStarts[pageStarts.Count - 1] >= pageSize)
                {
                    pageStarts.Add(i);
                    pageTitles.Add(title);
                    pageChapters.Add(chapter);
                }
            }
        }

        /// <summary>Index of the page that holds <paramref name="stage"/>.</summary>
        public static int PageOf(List<int> pageStarts, int stage)
        {
            int page = 0;
            while (page + 1 < pageStarts.Count && pageStarts[page + 1] <= stage)
            {
                page++;
            }

            return page;
        }

        /// <summary>The selection moved by <paramref name="delta"/> when it stays among the unlocked stages, else unchanged.</summary>
        public static int NextSelection(int selected, int delta, int unlocked)
        {
            int next = selected + delta;
            return next >= 0 && next < unlocked ? next : selected;
        }
    }
}
