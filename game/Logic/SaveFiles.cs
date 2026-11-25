// Purpose: keeps the game's saves as small text files on the phone, safely. A save is first
// written to a temporary file and only then swapped in, so a phone that dies mid-save leaves the
// last good save untouched. The save before is kept as a backup, for when the newest one won't
// load. The Unity scene passes in the folder (Application.persistentDataPath).
//
// The files are only stored here. Checking what's inside them is the rules' job
// (Campaign.load_text, ChapterRun.resume), which refuse anything damaged.
using System;
using System.IO;
using System.Text;

namespace Bodhi.Game
{
    public class SaveFiles
    {
        public const int largest_save_characters = 70000;  // More than either save can be.
        const int longest_slot_name = 40;

        // Text as UTF-8, without the invisible marker some editors put at the start.
        static readonly UTF8Encoding utf8 = new UTF8Encoding(false);

        readonly string folder;

        // Keeps saves in this folder, which must already exist.
        public SaveFiles(string folder)
        {
            this.folder = folder ?? throw new ArgumentNullException(nameof(folder));
        }

        // Writes a save, keeping the one before as a backup. False (and the old save kept) if
        // the name or text is refused or the phone won't write.
        public bool write(string slot_name, string text)
        {
            if (!is_good_name(slot_name) || text == null || text.Length > largest_save_characters)
            {
                return false;
            }
            string save_path = path_of(slot_name, ".save");
            string writing_path = path_of(slot_name, ".save.writing");
            try
            {
                // FileMode.Create replaces any half-written file a crash left behind.
                using (var stream = new FileStream(writing_path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] bytes = utf8.GetBytes(text);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);  // true: all the way to storage, not just to memory.
                }
                if (File.Exists(save_path))
                {
                    // Swaps the new file in and keeps the old one as the backup, in one step.
                    File.Replace(writing_path, save_path, path_of(slot_name, ".save.backup"));
                }
                else
                {
                    File.Move(writing_path, save_path);
                }
                return true;
            }
            catch (Exception problem) when (problem is IOException || problem is UnauthorizedAccessException || problem is NotSupportedException)
            {
                return false;
            }
        }

        // The newest save's text, or null if there's none or it can't be read.
        public string read(string slot_name)
        {
            return is_good_name(slot_name) ? read_path(path_of(slot_name, ".save")) : null;
        }

        // The save before the newest, or null.
        public string read_backup(string slot_name)
        {
            return is_good_name(slot_name) ? read_path(path_of(slot_name, ".save.backup")) : null;
        }

        // Removes a save and its backup, for example once a chapter is finished.
        public void erase(string slot_name)
        {
            if (!is_good_name(slot_name))
            {
                return;
            }
            foreach (var ending in new[] { ".save", ".save.backup", ".save.writing" })
            {
                try
                {
                    File.Delete(path_of(slot_name, ending));
                }
                catch (Exception problem) when (problem is IOException || problem is UnauthorizedAccessException)
                {
                    // Nothing to remove, or the phone won't let us; the save stays and is checked on load.
                }
            }
        }

        // Reads one file, refusing anything too big before reading it.
        static string read_path(string path)
        {
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists || info.Length > largest_save_characters * 4L)  // A character is at most 4 bytes.
                {
                    return null;
                }
                string text = File.ReadAllText(path, utf8);
                return text.Length > largest_save_characters ? null : text;
            }
            catch (Exception problem) when (problem is IOException || problem is UnauthorizedAccessException)
            {
                return null;
            }
        }

        // Only lowercase letters and underscores, so a name can never reach another folder.
        static bool is_good_name(string slot_name)
        {
            if (string.IsNullOrEmpty(slot_name) || slot_name.Length > longest_slot_name)
            {
                return false;
            }
            foreach (char letter in slot_name)
            {
                if (!(letter >= 'a' && letter <= 'z') && letter != '_')
                {
                    return false;
                }
            }
            return true;
        }

        // The full path of a slot's file with this ending.
        string path_of(string slot_name, string ending)
        {
            return Path.Combine(folder, slot_name + ending);
        }
    }
}
