namespace Prospace;

public class AppData
{
    public List<Subject> Subjects { get; set; } = new();
    public List<Note> Notes { get; set; } = new();
    public List<StudyTask> Tasks { get; set; } = new();
    public List<StudyEvent> Events { get; set; } = new();
    public List<SubjectFolder> Folders { get; set; } = new();
    public List<StudyFile> Files { get; set; } = new();
    public bool DarkMode { get; set; }
    public double UiScale { get; set; } = 1.0;
}

public class Subject
{
    public string Name { get; set; } = "";
}

public class SubjectFolder
{
    public string Subject { get; set; } = "";
    public string Name { get; set; } = "";
}

public class StudyFile
{
    public string Subject { get; set; } = "";
    public string? FolderName { get; set; }
    public string DisplayName { get; set; } = "";
    public string StoredPath { get; set; } = "";
}

public class Note
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class StudyTask
{
    public string Title { get; set; } = "";
    public DateTime DueDate { get; set; } = DateTime.Today;
    public string? Subject { get; set; }
    public string Notes { get; set; } = "";
}

public class StudyEvent
{
    public string Title { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.Today;
    public string Notes { get; set; } = "";
}

