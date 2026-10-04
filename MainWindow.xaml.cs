using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Prospace;

public partial class MainWindow : Window
{
    private readonly ProspaceDataStore _store = new();
    private AppData _data;
    private string _page = "Inicio";
    private DateTime _calendarDate = new(2026, 10, 3);
    private string _calendarMode = "Mes";
    private string? _selectedSubject;
    private string? _selectedFolder;
    private string? _selectedNoteId;
    private string? _editingNoteId;
    private TextBox? _noteTitleBox;
    private TextBox? _noteBodyBox;
    private DispatcherTimer? _noteSaveTimer;
    private static bool _darkMode;
    private DateTime _lastDate = DateTime.Today;

    public MainWindow()
    {
        InitializeComponent();
        _data = LoadData();
        _calendarDate = DateTime.Today;
        ApplyTheme();
        BuildNavigation();
        ShowPage("Inicio");
        Closing += (_, _) => FlushNoteEditor();
        Closing += (_, _) => FlushNoteEditor();
        var dateWatcher = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        dateWatcher.Tick += (_, _) =>
        {
            if (_lastDate == DateTime.Today) return;
            _lastDate = DateTime.Today;
            if (_page is "Eventos" or "Inicio" or "Calendario") ShowPage(_page);
        };
        dateWatcher.Start();
    }

    private AppData LoadData()
    {
        return _store.Load();
    }

    private void SaveData()
    {
        _store.Save(_data);
    }

    private void BuildNavigation()
    {
        foreach (var page in new[] { "Inicio", "Asignaturas", "Calendario", "Eventos", "Notas", "Tareas", "Ajustes" })
        {
            var button = new Button { Content = page, Tag = page, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(14, 12, 14, 12), Margin = new Thickness(0, 3, 0, 3), FontSize = 15, Foreground = Brushes.White, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
            button.Click += (_, _) => ShowPage((string)button.Tag);
            Navigation.Children.Add(button);
        }
    }

    private void ShowPage(string page)
    {
        FlushNoteEditor();
        _page = page;
        PageTitle.Text = page;
        PageSubtitle.Text = page switch
        {
            "Inicio" => "Un lugar para organizar tus estudios y tu día a día.",
            "Asignaturas" => "Organiza materias, carpetas y archivos de clase.",
            "Calendario" => "Eventos y tareas juntos, en vistas de mes, semana y día.",
            "Eventos" => "Próximos eventos. Los anteriores siguen guardados en el calendario.",
            "Notas" => "Tus notas personales, guardadas automáticamente.",
            "Ajustes" => "Personaliza el aspecto de Prospace.",
            _ => "Pequeños pasos para avanzar con tus objetivos."
        };
        ContentHost.Children.Clear();
        switch (page)
        {
            case "Inicio": ShowHome(); break;
            case "Asignaturas": ShowSubjects(); break;
            case "Eventos": ShowEvents(); break;
            case "Calendario": ShowCalendar(); break;
            case "Notas": ShowNotes(); break;
            case "Ajustes": ShowSettings(); break;
            default: ShowTasks(); break;
        }
    }

    private void ShowCalendar()
    {
        var toolbar = new WrapPanel { Margin = new Thickness(0, 0, 0, 14), VerticalAlignment = VerticalAlignment.Center };
        toolbar.Children.Add(ActionButton("‹", () => MoveCalendar(-1)));
        toolbar.Children.Add(ActionButton("Hoy", () => { _calendarDate = DateTime.Today; _calendarMode = "Mes"; ShowPage(_page); }));
        toolbar.Children.Add(ActionButton("›", () => MoveCalendar(1)));
        var zoomLabel = _calendarMode switch { "Mes" => "Ampliar a semana", "Semana" => "Ampliar a día", _ => "Volver a semana" };
        toolbar.Children.Add(ActionButton(zoomLabel, () =>
        {
            _calendarMode = _calendarMode switch { "Mes" => "Semana", "Semana" => "Dia", _ => "Semana" };
            ShowPage(_page);
        }));
        if (_calendarMode == "Dia") toolbar.Children.Add(ActionButton("Vista mensual", () => { _calendarMode = "Mes"; ShowPage(_page); }));
        ContentHost.Children.Add(toolbar);

        var monthStart = new DateTime(_calendarDate.Year, _calendarDate.Month, 1);
        var title = _calendarMode switch
        {
            "Mes" => monthStart.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-ES")),
            "Semana" => $"Semana del {_calendarDate.AddDays(-(((int)_calendarDate.DayOfWeek + 6) % 7)):d MMM}",
            _ => _calendarDate.ToString("dddd, d 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES"))
        };
        ContentHost.Children.Add(SectionLabel(title));
        ContentHost.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Children = { Legend("#8A79E6", "Eventos"), Legend("#34A889", "Tareas") } });
        if (_calendarMode == "Mes") DrawMonth(monthStart);
        else if (_calendarMode == "Semana") DrawWeek();
        else DrawDay(_calendarDate.Date);
    }

    private void MoveCalendar(int direction)
    {
        _calendarDate = _calendarMode switch
        {
            "Mes" => _calendarDate.AddMonths(direction),
            "Semana" => _calendarDate.AddDays(direction * 7),
            _ => _calendarDate.AddDays(direction)
        };
        ShowPage(_page);
    }

    private void DrawMonth(DateTime monthStart)
    {
        var weekdays = new UniformGrid { Columns = 7, Margin = new Thickness(0, 0, 0, 6) };
        foreach (var day in new[] { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" })
            weekdays.Children.Add(new TextBlock { Text = day, HorizontalAlignment = HorizontalAlignment.Center, Foreground = Brush("#788198"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 5, 0, 5) });
        ContentHost.Children.Add(weekdays);
        var grid = new UniformGrid { Columns = 7 };
        var offset = ((int)monthStart.DayOfWeek + 6) % 7;
        var start = monthStart.AddDays(-offset);
        for (var i = 0; i < 42; i++)
        {
            var date = start.AddDays(i);
            var stack = new StackPanel { Margin = new Thickness(5) };
            stack.Children.Add(new TextBlock { Text = date.Day.ToString(), FontSize = 14, FontWeight = date.Date == DateTime.Today ? FontWeights.Bold : FontWeights.Normal, Foreground = date.Month == monthStart.Month ? Brush("#343B52") : Brush("#AAB0C5") });
            var events = _data.Events.Where(e => e.Date.Date == date.Date).ToList();
            var tasks = _data.Tasks.Where(t => t.DueDate.Date == date.Date).ToList();
            foreach (var item in events.Take(2)) stack.Children.Add(MonthMarker("#8A79E6", item.Title));
            foreach (var item in tasks.Take(Math.Max(0, 2 - events.Count))) stack.Children.Add(MonthMarker("#34A889", item.Title));
            var more = events.Count + tasks.Count - 2;
            if (more > 0) stack.Children.Add(new TextBlock { Text = $"+{more}", Foreground = Brush("#788198"), FontSize = 10 });
            var cell = new Button { Content = stack, Height = 76, Margin = new Thickness(2), Padding = new Thickness(4), HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Top, Background = date.Date == _calendarDate.Date ? Brush("#E7EAFD") : Brush("White"), BorderBrush = Brush("#E4E7EF"), BorderThickness = new Thickness(1), Cursor = System.Windows.Input.Cursors.Hand };
            ApplyRounded(cell, 10);
            cell.Click += (_, _) => { _calendarDate = date; _calendarMode = "Dia"; ShowPage(_page); };
            grid.Children.Add(cell);
        }
        ContentHost.Children.Add(grid);
    }

    private void DrawWeek()
    {
        var monday = _calendarDate.AddDays(-(((int)_calendarDate.DayOfWeek + 6) % 7)).Date;
        for (var i = 0; i < 7; i++)
        {
            var date = monday.AddDays(i);
            var items = _data.Events.Where(e => e.Date.Date == date).Select(e => (e.Title, e.Notes, "#8A79E6"))
                .Concat(_data.Tasks.Where(t => t.DueDate.Date == date).Select(t => (t.Title, t.Notes, "#34A889"))).ToList();
            var summary = items.Count == 0 ? "Sin elementos" : string.Join("   ·   ", items.Take(3).Select(x => x.Title)) + (items.Count > 3 ? $"   +{items.Count - 3}" : "");
            var row = new Button { Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = date.ToString("ddd d", new System.Globalization.CultureInfo("es-ES")), Width = 100, FontWeight = FontWeights.SemiBold, Foreground = Brush("#343B52") }, new TextBlock { Text = summary, TextTrimming = TextTrimming.CharacterEllipsis, Foreground = Brush("#788198") } } }, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(14), Margin = new Thickness(0, 3, 0, 3), Background = date == DateTime.Today ? Brush("#E7EAFD") : Brush("White"), BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
            row.Click += (_, _) => { _calendarDate = date; _calendarMode = "Dia"; ShowPage(_page); };
            ContentHost.Children.Add(row);
        }
    }

    private void DrawDay(DateTime date)
    {
        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        actions.Children.Add(ActionButton("＋  Añadir evento", () => AddCalendarEvent(date)));
        actions.Children.Add(ActionButton("＋  Añadir tarea", () => AddCalendarTask(date)));
        ContentHost.Children.Add(actions);
        var events = _data.Events.Where(e => e.Date.Date == date).ToList();
        var tasks = _data.Tasks.Where(t => t.DueDate.Date == date).ToList();
        ContentHost.Children.Add(SectionLabel($"Eventos · {events.Count}"));
        if (events.Count == 0) ContentHost.Children.Add(Card("No hay eventos para este día."));
        foreach (var item in events) ContentHost.Children.Add(ItemDetail(item.Title, item.Notes, "#8A79E6"));
        ContentHost.Children.Add(SectionLabel($"Tareas · {tasks.Count}"));
        if (tasks.Count == 0) ContentHost.Children.Add(Card("No hay tareas para este día."));
        foreach (var item in tasks) ContentHost.Children.Add(ItemDetail(item.Title, item.Notes, "#34A889"));
    }

    private void AddCalendarEvent(DateTime date)
    {
        var selectedDate = PickDate(date);
        if (selectedDate is null) return;
        var title = Ask("Nuevo evento", "Nombre del evento o recordatorio:");
        if (string.IsNullOrWhiteSpace(title)) return;
        var notes = Ask("Detalles del evento", "Añade una nota o ubicación (opcional):");
        _data.Events.Add(new StudyEvent { Title = title.Trim(), Date = selectedDate.Value.Date, Notes = notes?.Trim() ?? "" }); SaveData(); ShowPage(_page);
    }

    private void AddCalendarTask(DateTime date)
    {
        var selectedDate = PickDate(date);
        if (selectedDate is null) return;
        var title = Ask("Nueva tarea", "¿Qué quieres hacer?");
        if (string.IsNullOrWhiteSpace(title)) return;
        var notes = Ask("Detalles de la tarea", "Añade un detalle o apunte (opcional):");
        _data.Tasks.Add(new StudyTask { Title = title.Trim(), DueDate = selectedDate.Value.Date, Notes = notes?.Trim() ?? "" }); SaveData(); ShowPage(_page);
    }

    private static TextBlock Legend(string color, string label) => new() { Text = "●  " + label + "     ", Foreground = Brush(color), Margin = new Thickness(0, 0, 0, 12) };
    private static TextBlock MonthMarker(string color, string title) => new() { Text = "● " + title, Foreground = Brush(color), FontSize = 9, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 4, 0, 0) };
    private Border ItemDetail(string title, string notes, string color)
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = "●  " + title, FontWeight = FontWeights.SemiBold, FontSize = 15, Foreground = Brush(color) });
        if (!string.IsNullOrWhiteSpace(notes)) panel.Children.Add(new TextBlock { Text = notes, TextWrapping = TextWrapping.Wrap, Foreground = Brush("#788198"), Margin = new Thickness(23, 7, 0, 0) });
        return new Border { Child = panel, Background = Brush("White"), CornerRadius = new CornerRadius(10), Padding = new Thickness(16), Margin = new Thickness(0, 0, 0, 10) };
    }

    private DateTime? PickDate(DateTime initialDate)
    {
        var dialog = new Window { Title = "Elige una fecha", Owner = this, Width = 390, Height = 440, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize, Background = Brush("#F5F6FA") };
        var content = new StackPanel { Margin = new Thickness(18) };
        var month = new DateTime(initialDate.Year, initialDate.Month, 1);
        var nav = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
        var monthLabel = new TextBlock { FontSize = 17, FontWeight = FontWeights.SemiBold, Foreground = Brush("#20263B"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var days = new UniformGrid { Columns = 7 };
        DateTime? selected = null;
        var footer = new TextBlock { Text = "Selecciona un día", Foreground = Brush("#788198"), Margin = new Thickness(0, 12, 0, 8), HorizontalAlignment = HorizontalAlignment.Center };
        var previous = SmallButton("‹", () => { month = month.AddMonths(-1); RenderMonth(); });
        var next = SmallButton("›", () => { month = month.AddMonths(1); RenderMonth(); });
        DockPanel.SetDock(next, Dock.Right); DockPanel.SetDock(previous, Dock.Left);
        nav.Children.Add(next); nav.Children.Add(previous); nav.Children.Add(monthLabel); content.Children.Add(nav);
        var weekdayRow = new UniformGrid { Columns = 7, Margin = new Thickness(0, 0, 0, 4) };
        foreach (var name in new[] { "L", "M", "X", "J", "V", "S", "D" }) weekdayRow.Children.Add(new TextBlock { Text = name, HorizontalAlignment = HorizontalAlignment.Center, Foreground = Brush("#788198"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 4, 0, 4) });
        content.Children.Add(weekdayRow);
        content.Children.Add(days);
        content.Children.Add(footer);
        var cancel = SmallButton("Cancelar", () => dialog.DialogResult = false);
        cancel.HorizontalAlignment = HorizontalAlignment.Center; content.Children.Add(cancel);
        dialog.Content = content;

        void RenderMonth()
        {
            monthLabel.Text = month.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-ES"));
            days.Children.Clear();
            var first = month.AddDays(-(((int)month.DayOfWeek + 6) % 7));
            for (var i = 0; i < 42; i++)
            {
                var day = first.AddDays(i);
                var button = new Button { Content = day.Day.ToString(), Height = 39, Margin = new Thickness(2), Background = Brush("White"), Foreground = day.Month == month.Month ? Brush("#343B52") : Brush("#AAB0C5"), BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
                ApplyRounded(button, 12);
                button.Click += (_, _) => { selected = day.Date; footer.Text = day.ToString("dddd, d 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES")); dialog.DialogResult = true; };
                days.Children.Add(button);
            }
        }

        RenderMonth();
        return dialog.ShowDialog() == true ? selected : null;
    }

    private void ShowSettings()
    {
        ContentHost.Children.Add(SectionLabel("Apariencia"));
        ContentHost.Children.Add(Card("Elige un tema para trabajar cómodamente."));
        ContentHost.Children.Add(ActionButton(_data.DarkMode ? "☀  Cambiar a modo claro" : "☾  Activar modo oscuro", () =>
        {
            _data.DarkMode = !_data.DarkMode; SaveData(); ApplyTheme(); ShowPage(_page);
        }));
        ContentHost.Children.Add(SectionLabel("Tamaño de la interfaz"));
        ContentHost.Children.Add(Card("Elige el tamaño que te resulte más cómodo para tu pantalla."));
        var sizes = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var (label, scale) in new[] { ("Compacto", 0.9), ("Normal", 1.0), ("Grande", 1.15) })
            sizes.Children.Add(ActionButton(label + (_data.UiScale == scale ? "  ✓" : ""), () =>
            {
                _data.UiScale = scale; RootLayout.LayoutTransform = new ScaleTransform(scale, scale); SaveData(); ShowPage(_page);
            }));
        ContentHost.Children.Add(sizes);
    }

    private void ApplyTheme()
    {
        _darkMode = _data.DarkMode;
        RootLayout.Background = Brush("#F5F6FA"); Sidebar.Background = _data.DarkMode ? Brush("#111522") : Brush("#20263B");
        PageTitle.Foreground = Brush("#20263B"); PageSubtitle.Foreground = Brush("#788198");
        RootLayout.LayoutTransform = new ScaleTransform(_data.UiScale, _data.UiScale);
    }

    private void ShowHome()
    {
        var today = DateTime.Today;
        ContentHost.Children.Add(Card($"Hola 👋\n\nHoy es {today:dddd, d 'de' MMMM}. Este es tu espacio para estudiar con más calma."));
        ContentHost.Children.Add(SectionLabel("Tu espacio ahora"));
        var row = new WrapPanel();
        row.Children.Add(StatCard("Asignaturas", _data.Subjects.Count.ToString()));
        row.Children.Add(StatCard("Notas guardadas", _data.Notes.Count.ToString()));
        row.Children.Add(StatCard("Tareas pendientes", _data.Tasks.Count.ToString()));
        row.Children.Add(StatCard("Próximos eventos", _data.Events.Count(e => e.Date.Date >= today).ToString()));
        ContentHost.Children.Add(row);
        ContentHost.Children.Add(SectionLabel("Próximos eventos"));
        var upcomingEvents = _data.Events.Where(e => e.Date.Date >= today).OrderBy(e => e.Date).Take(4).ToList();
        if (upcomingEvents.Count == 0) ContentHost.Children.Add(Card("No tienes próximos eventos."));
        foreach (var item in upcomingEvents) ContentHost.Children.Add(Card($"{item.Date:ddd, d MMM}   ·   {item.Title}"));
        ContentHost.Children.Add(SectionLabel("Próximas tareas"));
        var upcoming = _data.Tasks.OrderBy(t => t.DueDate).Take(4).ToList();
        if (upcoming.Count == 0) ContentHost.Children.Add(Card("Todavía no tienes tareas. En Tareas puedes añadir objetivos para la semana."));
        foreach (var task in upcoming) ContentHost.Children.Add(Card($"{task.DueDate:ddd, d MMM}   ·   {task.Title}{(string.IsNullOrWhiteSpace(task.Subject) ? "" : "   ·   " + task.Subject)}"));
    }

    private void ShowSubjects()
    {
        if (_selectedSubject is not null)
        {
            ShowSubjectContents();
            return;
        }
        ContentHost.Children.Add(ActionButton("＋  Añadir asignatura", () =>
        {
            var name = Ask("Nueva asignatura", "¿Cómo se llama la asignatura?");
            if (!string.IsNullOrWhiteSpace(name)) { _data.Subjects.Add(new Subject { Name = name.Trim() }); SaveData(); ShowPage(_page); }
        }));
        if (_data.Subjects.Count == 0) ContentHost.Children.Add(Card("Aún no hay asignaturas. Añade la primera para empezar a organizar tus clases."));
        var cards = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
        ContentHost.Children.Add(cards);
        foreach (var subject in _data.Subjects)
        {
            var folderCount = _data.Folders.Count(f => f.Subject == subject.Name);
            var fileCount = _data.Files.Count(f => f.Subject == subject.Name);
            var subjectContent = new StackPanel();
            subjectContent.Children.Add(new TextBlock { Text = "📁  " + subject.Name, FontSize = 18, FontWeight = FontWeights.SemiBold, Foreground = Brush("#20263B"), TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 0, 34, 0) });
            subjectContent.Children.Add(new TextBlock { Text = $"{folderCount} carpetas   ·   {fileCount} archivos", FontSize = 13, Foreground = Brush("#788198"), Margin = new Thickness(31, 8, 0, 0) });
            var open = new Button { Content = subjectContent, HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center, Width = 284, Height = 112, Padding = new Thickness(18), Background = Brush("#EEF1FF"), Foreground = Brush("#343B52"), BorderBrush = Brush("#DCE2FF"), BorderThickness = new Thickness(1), Cursor = System.Windows.Input.Cursors.Hand };
            ApplyRounded(open, 18);
            open.Click += (_, _) =>
            {
                _selectedSubject = subject.Name; _selectedFolder = null; ShowPage("Asignaturas");
            };
            var menuButton = SmallButton("···", () => { });
            menuButton.FontSize = 18; menuButton.Width = 36; menuButton.Height = 32;
            var menu = new ContextMenu { Background = Brush("White"), Foreground = Brush("#343B52") };
            var rename = new MenuItem { Header = "Editar nombre", Foreground = Brush("#343B52") };
            rename.Click += (_, _) => RenameSubject(subject);
            var delete = new MenuItem { Header = "Borrar", Foreground = Brush("#343B52") };
            delete.Click += (_, _) => DeleteSubject(subject);
            menu.Items.Add(rename); menu.Items.Add(delete);
            menuButton.ContextMenu = menu;
            menuButton.Click += (_, _) => { menu.PlacementTarget = menuButton; menu.IsOpen = true; };
            var tile = new Grid { Width = 300, Height = 112, Margin = new Thickness(0, 0, 14, 14) };
            tile.Children.Add(open);
            menuButton.HorizontalAlignment = HorizontalAlignment.Right; menuButton.VerticalAlignment = VerticalAlignment.Top; menuButton.Margin = new Thickness(0, 8, 8, 0);
            tile.Children.Add(menuButton);
            cards.Children.Add(tile);
        }
    }

    private void RenameSubject(Subject subject)
    {
        var name = Ask("Editar asignatura", "Nuevo nombre para " + subject.Name + ":");
        if (string.IsNullOrWhiteSpace(name)) return;
        var oldName = subject.Name; subject.Name = name.Trim();
        foreach (var task in _data.Tasks.Where(t => t.Subject == oldName)) task.Subject = subject.Name;
        foreach (var folder in _data.Folders.Where(f => f.Subject == oldName)) folder.Subject = subject.Name;
        foreach (var file in _data.Files.Where(f => f.Subject == oldName)) file.Subject = subject.Name;
        SaveData(); ShowPage(_page);
    }

    private void DeleteSubject(Subject subject)
    {
        var answer = MessageBox.Show($"¿Borrar «{subject.Name}» y sus carpetas y archivos asociados?", "Borrar asignatura", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;
        var files = _data.Files.Where(f => f.Subject == subject.Name).ToList();
        foreach (var file in files)
        {
            try { if (File.Exists(file.StoredPath)) File.Delete(file.StoredPath); } catch { }
            _data.Files.Remove(file);
        }
        _data.Folders.RemoveAll(f => f.Subject == subject.Name);
        foreach (var task in _data.Tasks.Where(t => t.Subject == subject.Name)) task.Subject = null;
        _data.Subjects.Remove(subject);
        _selectedSubject = null; _selectedFolder = null;
        SaveData(); ShowPage("Asignaturas");
    }

    private void ShowSubjectContents()
    {
        var subject = _data.Subjects.FirstOrDefault(s => s.Name == _selectedSubject);
        if (subject is null) { _selectedSubject = null; ShowSubjects(); return; }
        ContentHost.Children.Add(ActionButton("‹  Todas las asignaturas", () => { _selectedSubject = null; _selectedFolder = null; ShowPage("Asignaturas"); }));
        ContentHost.Children.Add(SectionLabel("📁  " + subject.Name));
        var folderNav = new WrapPanel();
        folderNav.Children.Add(ActionButton("Archivos generales", () => { _selectedFolder = null; ShowPage(_page); }));
        foreach (var folder in _data.Folders.Where(f => f.Subject == subject.Name))
            folderNav.Children.Add(ActionButton("📂  " + folder.Name, () => { _selectedFolder = folder.Name; ShowPage(_page); }));
        ContentHost.Children.Add(folderNav);

        if (_selectedFolder is null)
        {
            ContentHost.Children.Add(ActionButton("＋  Crear carpeta", () =>
            {
                var name = Ask("Nueva carpeta", "Nombre de la carpeta:");
                if (string.IsNullOrWhiteSpace(name)) return;
                if (_data.Folders.Any(f => f.Subject == subject.Name && f.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
                { MessageBox.Show("Ya existe una carpeta con ese nombre en esta asignatura."); return; }
                _data.Folders.Add(new SubjectFolder { Subject = subject.Name, Name = name.Trim() }); SaveData(); ShowPage(_page);
            }));
        }
        else
        {
            ContentHost.Children.Add(SectionLabel("Carpeta · " + _selectedFolder));
        }

        var folderName = _selectedFolder;
        ContentHost.Children.Add(ActionButton("＋  Añadir archivos de mi equipo", () => ImportFiles(subject.Name, folderName)));

        var files = _data.Files.Where(f => f.Subject == subject.Name && f.FolderName == folderName).ToList();
        ContentHost.Children.Add(SectionLabel("Archivos"));
        if (files.Count == 0) ContentHost.Children.Add(Card("Aún no has añadido archivos aquí."));
        foreach (var file in files) ContentHost.Children.Add(FileCard(file));
    }

    private void ImportFiles(string subject, string? folder)
    {
        var picker = new OpenFileDialog { Title = "Añadir archivos a Prospace", Multiselect = true, CheckFileExists = true };
        if (picker.ShowDialog(this) != true) return;
        var storage = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Prospace", "Files");
        Directory.CreateDirectory(storage);
        foreach (var source in picker.FileNames)
        {
            var storedName = Guid.NewGuid().ToString("N") + Path.GetExtension(source);
            var destination = Path.Combine(storage, storedName);
            File.Copy(source, destination);
            _data.Files.Add(new StudyFile { Subject = subject, FolderName = folder, DisplayName = Path.GetFileName(source), StoredPath = destination });
        }
        SaveData(); ShowPage(_page);
    }

    private Border FileCard(StudyFile file)
    {
        var row = new DockPanel();
        var open = SmallButton("Abrir", () =>
        {
            if (File.Exists(file.StoredPath)) Process.Start(new ProcessStartInfo(file.StoredPath) { UseShellExecute = true });
            else MessageBox.Show("No se encuentra la copia guardada de este archivo.");
        });
        DockPanel.SetDock(open, Dock.Right); row.Children.Add(open);
        row.Children.Add(new TextBlock { Text = "📎  " + file.DisplayName, VerticalAlignment = VerticalAlignment.Center, Foreground = Brush("#343B52"), TextTrimming = TextTrimming.CharacterEllipsis });
        return new Border { Child = row, Background = Brush("White"), CornerRadius = new CornerRadius(10), Padding = new Thickness(14), Margin = new Thickness(0, 0, 0, 8) };
    }

    private Border NoteCard(Note note)
    {
        var row = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
        var delete = SmallButton("Borrar", () => DeleteNote(note));
        DockPanel.SetDock(delete, Dock.Right); row.Children.Add(delete);
        var preview = new StackPanel();
        preview.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(note.Title) ? "Sin título" : note.Title, FontSize = 17, FontWeight = FontWeights.SemiBold, Foreground = Brush("#20263B"), TextTrimming = TextTrimming.CharacterEllipsis });
        preview.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(note.Body) ? "Sin contenido" : note.Body.Replace("\r", " ").Replace("\n", " "), FontSize = 13, Foreground = Brush("#788198"), TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 6, 0, 0) });
        preview.Children.Add(new TextBlock { Text = note.CreatedAt.ToString("d MMM yyyy"), FontSize = 11, Foreground = Brush("#788198"), Margin = new Thickness(0, 7, 0, 0) });
        var open = new Button { Content = preview, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(17), Background = Brush("White"), BorderBrush = Brush("#E4E7EF"), BorderThickness = new Thickness(1), Cursor = System.Windows.Input.Cursors.Hand, Margin = new Thickness(0, 0, 8, 0) };
        ApplyRounded(open, 16);
        open.Click += (_, _) => OpenNote(note);
        row.Children.Add(open);
        return new Border { Child = row, Background = Brushes.Transparent };
    }

    private void DeleteNote(Note note)
    {
        if (MessageBox.Show($"¿Borrar la nota «{note.Title}»?", "Borrar nota", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        if (_selectedNoteId == note.Id) _selectedNoteId = null;
        _data.Notes.Remove(note); SaveData(); ShowPage(_page);
    }

    private Button SmallButton(string label, Action action)
    {
        var button = new Button { Content = label, Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(4, 0, 0, 4), Background = Brush("#E7EAFD"), Foreground = Brush("#4654C1"), BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
        ApplyRounded(button, 12);
        button.Click += (_, _) => action(); return button;
    }

    private void ShowNotes()
    {
        var selected = _data.Notes.FirstOrDefault(n => n.Id == _selectedNoteId);
        if (selected is not null)
        {
            ShowNoteEditor(selected);
            return;
        }
        _selectedNoteId = null;
        ContentHost.Children.Add(ActionButton("＋  Nueva nota", CreateNote));
        if (_data.Notes.Count == 0) ContentHost.Children.Add(Card("Todavía no tienes notas. Crea una para empezar a escribir."));
        foreach (var note in _data.Notes.OrderByDescending(n => n.CreatedAt)) ContentHost.Children.Add(NoteCard(note));
    }

    private void CreateNote()
    {
        var note = new Note { Title = "Nueva nota", Body = "" };
        _data.Notes.Add(note);
        _selectedNoteId = note.Id;
        SaveData(); ShowPage("Notas");
    }

    private void OpenNote(Note note)
    {
        _selectedNoteId = note.Id;
        ShowPage("Notas");
    }

    private void ShowNoteEditor(Note note)
    {
        ContentHost.Children.Add(ActionButton("‹  Todas las notas", () =>
        {
            FlushNoteEditor();
            _selectedNoteId = null;
            ShowPage("Notas");
        }));
        var titleBox = new TextBox { Text = note.Title, FontSize = 24, FontWeight = FontWeights.SemiBold, Foreground = Brush("#20263B"), Background = Brush("White"), BorderThickness = new Thickness(0), Padding = new Thickness(15, 12, 15, 12), Margin = new Thickness(0, 5, 0, 10) };
        ApplyRoundedTextBox(titleBox);
        ContentHost.Children.Add(titleBox);
        var bodyBox = new TextBox { Text = note.Body, FontSize = 16, Foreground = Brush("#343B52"), Background = Brush("White"), BorderThickness = new Thickness(0), Padding = new Thickness(18), AcceptsReturn = true, AcceptsTab = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MinHeight = 360, Height = 440, VerticalContentAlignment = VerticalAlignment.Top };
        ApplyRoundedTextBox(bodyBox);
        ContentHost.Children.Add(bodyBox);
        ContentHost.Children.Add(new TextBlock { Text = "Se guarda automáticamente", Foreground = Brush("#788198"), FontSize = 12, Margin = new Thickness(5, 10, 0, 0) });

        _editingNoteId = note.Id;
        _noteTitleBox = titleBox;
        _noteBodyBox = bodyBox;
        titleBox.TextChanged += (_, _) => ScheduleNoteSave();
        bodyBox.TextChanged += (_, _) => ScheduleNoteSave();
    }

    private static void ApplyRoundedTextBox(TextBox box)
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(16));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        var content = new FrameworkElementFactory(typeof(ScrollViewer));
        content.Name = "PART_ContentHost";
        border.AppendChild(content);
        box.Template = new ControlTemplate(typeof(TextBox)) { VisualTree = border };
    }

    private void ScheduleNoteSave()
    {
        var note = _data.Notes.FirstOrDefault(n => n.Id == _editingNoteId);
        if (note is null || _noteTitleBox is null || _noteBodyBox is null) return;
        note.Title = _noteTitleBox.Text;
        note.Body = _noteBodyBox.Text;
        _noteSaveTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _noteSaveTimer.Stop();
        _noteSaveTimer.Tick -= NoteSaveTimer_Tick;
        _noteSaveTimer.Tick += NoteSaveTimer_Tick;
        _noteSaveTimer.Start();
    }

    private void NoteSaveTimer_Tick(object? sender, EventArgs e)
    {
        _noteSaveTimer?.Stop();
        PersistNoteEditor();
    }

    private void PersistNoteEditor()
    {
        var note = _data.Notes.FirstOrDefault(n => n.Id == _editingNoteId);
        if (note is null || _noteTitleBox is null || _noteBodyBox is null) return;
        note.Title = _noteTitleBox.Text;
        note.Body = _noteBodyBox.Text;
        SaveData();
    }

    private void FlushNoteEditor()
    {
        _noteSaveTimer?.Stop();
        PersistNoteEditor();
        _noteTitleBox = null;
        _noteBodyBox = null;
        _editingNoteId = null;
    }

    private void ShowTasks()
    {
        ContentHost.Children.Add(ActionButton("＋  Añadir tarea", () =>
        {
            var title = Ask("Nueva tarea", "¿Qué quieres hacer?");
            if (string.IsNullOrWhiteSpace(title)) return;
            var date = PickDate(DateTime.Today);
            if (date is null) return;
            var notes = Ask("Detalles de la tarea", "Añade un detalle o apunte (opcional):");
            _data.Tasks.Add(new StudyTask { Title = title.Trim(), DueDate = date.Value.Date, Notes = notes?.Trim() ?? "" }); SaveData(); ShowPage(_page);
        }));
        if (_data.Tasks.Count == 0) ContentHost.Children.Add(Card("¡Todo al día! Añade una tarea cuando quieras planificar algo."));
        foreach (var task in _data.Tasks.OrderBy(t => t.DueDate).ToList())
        {
            var row = new DockPanel { Margin = new Thickness(0, 4, 0, 4) };
            var done = new Button { Content = "✓ Hecha", Padding = new Thickness(12, 8, 12, 8), Background = Brush("#E7EAFD"), Foreground = Brush("#4654C1"), BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
            done.Click += (_, _) => { _data.Tasks.Remove(task); SaveData(); ShowPage(_page); };
            DockPanel.SetDock(done, Dock.Right); row.Children.Add(done);
            var text = Card($"{task.Title}\n{task.DueDate:dddd, d MMMM yyyy}"); text.Margin = new Thickness(0, 0, 12, 0); row.Children.Add(text);
            ContentHost.Children.Add(row);
        }
    }

    private void ShowEvents()
    {
        ContentHost.Children.Add(ActionButton("＋  Añadir evento", () =>
        {
            var title = Ask("Nuevo evento", "Nombre del evento:");
            if (string.IsNullOrWhiteSpace(title)) return;
            var date = PickDate(DateTime.Today);
            if (date is null) return;
            var notes = Ask("Detalles del evento", "Añade una nota o ubicación (opcional):");
            _data.Events.Add(new StudyEvent { Title = title.Trim(), Date = date.Value.Date, Notes = notes?.Trim() ?? "" }); SaveData(); ShowPage(_page);
        }));
        var events = _data.Events.Where(e => e.Date.Date >= DateTime.Today).OrderBy(e => e.Date).ToList();
        if (events.Count == 0) ContentHost.Children.Add(Card("No tienes próximos eventos. Los eventos anteriores siguen disponibles en el calendario."));
        foreach (var item in events)
        {
            var row = new DockPanel { Margin = new Thickness(0, 4, 0, 4) };
            var edit = new Button { Content = "Editar", Padding = new Thickness(12, 8, 12, 8), Background = Brush("#E7EAFD"), Foreground = Brush("#4654C1"), BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
            edit.Click += (_, _) =>
            {
                var title = Ask("Editar evento", "Nombre del evento:");
                if (string.IsNullOrWhiteSpace(title)) return;
                var date = PickDate(item.Date);
                if (date is null) return;
                item.Title = title.Trim(); item.Date = date.Value.Date; SaveData(); ShowPage(_page);
            };
            DockPanel.SetDock(edit, Dock.Right); row.Children.Add(edit);
            var card = Card($"{item.Date:dddd, d MMMM yyyy}\n{item.Title}"); card.Margin = new Thickness(0, 0, 12, 0); row.Children.Add(card);
            ContentHost.Children.Add(row);
        }
    }

    private static Border Card(string text) => new() { Child = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = 15, Foreground = Brush("#343B52"), LineHeight = 23 }, Background = Brush("White"), CornerRadius = new CornerRadius(18), Padding = new Thickness(20), Margin = new Thickness(0, 0, 0, 12) };
    private static TextBlock SectionLabel(string text) => new() { Text = text, FontSize = 18, FontWeight = FontWeights.SemiBold, Foreground = Brush("#20263B"), Margin = new Thickness(0, 22, 0, 12) };
    private static Border StatCard(string label, string value) => new() { Child = new StackPanel { Children = { new TextBlock { Text = value, FontSize = 27, FontWeight = FontWeights.Bold, Foreground = Brush("#5967D8") }, new TextBlock { Text = label, FontSize = 13, Foreground = Brush("#788198"), Margin = new Thickness(0, 4, 0, 0) } } }, Background = Brush("White"), CornerRadius = new CornerRadius(16), Padding = new Thickness(20), Margin = new Thickness(0, 0, 12, 0), MinWidth = 150 };
    private static Button ActionButton(string text, Action action) { var button = new Button { Content = text, HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(18, 11, 18, 11), Margin = new Thickness(0, 0, 0, 8), Background = Brush("#5967D8"), Foreground = Brushes.White, FontSize = 14, FontWeight = FontWeights.SemiBold, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand }; ApplyRounded(button, 14); button.Click += (_, _) => action(); return button; }
    private static void ApplyRounded(Button button, double radius)
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.Name = "ButtonBorder";
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
        border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        border.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        border.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetBinding(ContentPresenter.ContentProperty, new System.Windows.Data.Binding("Content") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        presenter.SetBinding(ContentPresenter.ContentTemplateProperty, new System.Windows.Data.Binding("ContentTemplate") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        presenter.SetBinding(ContentPresenter.HorizontalAlignmentProperty, new System.Windows.Data.Binding("HorizontalContentAlignment") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        presenter.SetBinding(ContentPresenter.VerticalAlignmentProperty, new System.Windows.Data.Binding("VerticalContentAlignment") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        presenter.SetValue(ContentPresenter.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
        border.AppendChild(presenter);
        var template = new ControlTemplate(typeof(Button)) { VisualTree = border };
        button.Template = template;
    }
    private static SolidColorBrush Brush(string color)
    {
        if (_darkMode)
            color = color.ToUpperInvariant() switch
            {
                "#F5F6FA" => "#171B28", "#FFFFFF" or "WHITE" => "#252B3B",
                "#20263B" => "#F2F4FA", "#343B52" => "#E1E5F0",
                "#788198" => "#ADB5C7", "#AAB0C5" => "#7E879D",
                "#E7EAFD" => "#323954", "#4654C1" => "#C3CAFF",
                "#EEF1FF" => "#292F44", "#DCE2FF" => "#3C4560",
                _ => color
            };
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }

    private string? Ask(string title, string prompt)
    {
        var dialog = new Window { Title = title, Owner = this, Width = 390, Height = 210, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize, Background = Brush("#F5F6FA") };
        var panel = new StackPanel { Margin = new Thickness(22) };
        panel.Children.Add(new TextBlock { Text = prompt, TextWrapping = TextWrapping.Wrap, Foreground = Brush("#343B52"), Margin = new Thickness(0, 0, 0, 12) });
        var input = new TextBox { Padding = new Thickness(9), FontSize = 14 }; panel.Children.Add(input);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 18, 0, 0) };
        var cancel = new Button { Content = "Cancelar", Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 8, 0), IsCancel = true };
        var save = new Button { Content = "Guardar", Padding = new Thickness(12, 7, 12, 7), IsDefault = true, Background = Brush("#5967D8"), Foreground = Brushes.White };
        buttons.Children.Add(cancel); buttons.Children.Add(save); panel.Children.Add(buttons); dialog.Content = panel;
        save.Click += (_, _) => dialog.DialogResult = true;
        return dialog.ShowDialog() == true ? input.Text : null;
    }
}

