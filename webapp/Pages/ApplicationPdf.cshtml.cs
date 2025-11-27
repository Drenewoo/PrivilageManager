using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.IO;
using DinkToPdf;
using DinkToPdf.Contracts;
using webapp.Data;
using webapp.Models;

//using Orientation = MigraDocCore.DocumentObjectModel.Orientation;
//using Unit = MigraDocCore.DocumentObjectModel.Unit;

namespace webapp.Pages;

public class ApplicationPdf : PageModel
{
    private readonly IRazorViewToStringRenderer _razorRenderer;
    private readonly IConverter _pdfConverter;
    public ApplicationData data;
    private readonly AppDbContext _context;
    
    public ApplicationPdf(IRazorViewToStringRenderer razorRenderer, IConverter pdfConverter, AppDbContext context)
    {
        _razorRenderer = razorRenderer;
        _pdfConverter = pdfConverter;
        _context = context;
    }

    public IActionResult OnGet()
    {
        var id = HttpContext.Session.GetInt32("PDFApplicationId") ?? 0;
        id = 1;
        if (id == 0)
            return BadRequest();
        data = GetApplicationDataFromDb(id);
        return Page();
    }
    public async Task<IActionResult> OnGetPdfAsync()
    {
        
        var id = HttpContext.Session.GetInt32("PDFApplicationId") ?? 0;
        if (id == 0)
            return BadRequest();
        // Tu pobierasz dane z bazy po id
        var data = GetApplicationDataFromDb(id);

        /*var pdfBytes = PdfGenerator.Generate(data);
        return File(pdfBytes, "application/pdf");
    */
        string html = await _razorRenderer.RenderViewToStringAsync("/Views/Shared/ApplicationPdfView.cshtml", data);

        // 2. Konfiguracja PDF
        var doc = new HtmlToPdfDocument()
        {
            GlobalSettings = new GlobalSettings
            {
                PaperSize = PaperKind.A4,
                Orientation = Orientation.Portrait
            },
            Objects = { new ObjectSettings { HtmlContent = html } }
        };

        // 3. Generowanie PDF
        var pdf = _pdfConverter.Convert(doc);

        return File(pdf, "application/pdf");
    }

    private ApplicationData GetApplicationDataFromDb(int id)
    {

        var applicationDetails = _context.ApplicationDetails.FirstOrDefault(a => a.ApplicationId == id);
        var messageLinks = _context.MessageLinks.Where(a => a.ApplicationId == id).ToList();
        List<Message> messages = new List<Message>();
        foreach (var ms in messageLinks)
        {
            if (_context.Messages.Any(m => m.Id == ms.Id))
            {
                messages.Add(_context.Messages.First(m => m.Id == ms.Id));
            }
        }

        var rDate = messages.Min(m => m.RequestDate).Date;
        var devM = messages.Where(m => m.DeviceId != null);
        var proM = messages.Where(m => m.PermissionId != null);
        var who = _context.Users.FirstOrDefault(u =>
            u.Id == _context.ActionHistories.OrderBy(d => d.Date)
                .FirstOrDefault(e => e.ApplicationId == id && e.ActionId == 1).UserId);
        var forU = _context.Users.FirstOrDefault(u => u.Id == messages.ElementAt(0).UserId);
        var adm = _context.Users.FirstOrDefault(u =>
            u.Id == _context.ActionHistories.OrderByDescending(d => d.Date)
                .FirstOrDefault(e => e.ApplicationId == id && e.ActionId == 6).UserId);
        var iod = _context.Users.FirstOrDefault(u =>
            u.Id == _context.ActionHistories.OrderByDescending(d => d.Date)
                .FirstOrDefault(e => e.ApplicationId == id && e.ActionId == 5).UserId);
        List<PermissionModel> perms = new List<PermissionModel>();
        foreach (var m in proM)
        {
            var perm = new PermissionModel();
            perm.Program = _context.Programs.FirstOrDefault(p => p.Id == m.ProgramId)?.Name ?? String.Empty;
            perm.Permission = _context.Permissions.FirstOrDefault(p => p.Id == m.PermissionId)?.Name ?? String.Empty;
            perm.Login = _context.Logins.FirstOrDefault(l => l.UserId == forU.Id && l.ProgramId == m.ProgramId)
                .Username;
            perms.Add(perm);
        }

        List<DeviceModel> devices = new List<DeviceModel>();
        foreach (var m in devM)
        {
            var dev = new DeviceModel();
            dev.Name = _context.Devices.FirstOrDefault(d => d.Id == m.DeviceId)?.Name ?? String.Empty;
            dev.Serial = _context.Devices.FirstOrDefault(d => d.Id == m.DeviceId)?.Serial ?? String.Empty;
            devices.Add(dev);
        }
        
        return new ApplicationData
        {
            ApplicationId = id,
            ApplicationDate = rDate,
            WhoFName = who?.FirstName ?? String.Empty,
            WhoLName = who?.LastName ?? String.Empty,
            Degree = _context.Degrees.FirstOrDefault(d => d.Id == who.DegreeId)?.Name ?? String.Empty,
            Department = _context.Departments.FirstOrDefault(e => e.Id == _context.Degrees.First(d => d.Id == who.DegreeId).DepartmentId)?.Name ?? String.Empty,
            ForFName = forU?.FirstName ?? String.Empty,
            ForLName = forU?.LastName ?? String.Empty,
            DegreeFor = _context.Degrees.FirstOrDefault(d => d.Id == forU.DegreeId)?.Name ?? String.Empty,
            DepartmentFor = _context.Departments.FirstOrDefault(e => e.Id == _context.Degrees.First(d => d.Id == forU.DegreeId).DepartmentId)?.Name ?? String.Empty,
            MessageText = applicationDetails?.Message ?? "",
            ExpireDate = applicationDetails?.ExpireDate == DateTime.UnixEpoch || applicationDetails?.ExpireDate == null ? "Brak" : applicationDetails?.ExpireDate.ToString("dd-MM-yyyy"),
            Permissions = perms,
            Devices = devices,
            AdmFName = adm?.FirstName ?? String.Empty,
            AdmLName = adm?.LastName ?? String.Empty,
            IodFName = iod?.FirstName ?? String.Empty,
            IodLName = iod?.LastName ?? String.Empty,
            DateNow = DateTime.Now
        };
    }
}
public class ApplicationData
{
    public int ApplicationId { get; set; }
    public DateTime ApplicationDate { get; set; }
    public string WhoFName { get; set; }
    public string WhoLName { get; set; }
    public string Degree { get; set; }
    public string Department { get; set; }
    public string ForFName { get; set; }
    public string ForLName { get; set; }
    public string DegreeFor { get; set; }
    public string DepartmentFor { get; set; }
    public string MessageText { get; set; }
    public string ExpireDate { get; set; }
    public List<PermissionModel> Permissions { get; set; } = new();
    public List<DeviceModel> Devices { get; set; } = new();
    public string AdmFName { get; set; }
    public string AdmLName { get; set; }
    public string IodFName { get; set; }
    public string IodLName { get; set; }
    public DateTime DateNow { get; set; }
}

public class PermissionModel
{
    public string Program { get; set; }
    public string Permission { get; set; }
    public string Login { get; set; }
}

public class DeviceModel
{
    public string Name { get; set; }
    public string Serial { get; set; }
}
/*public static class PdfGenerator
{
    public static byte[] Generate(ApplicationData data)
    {
        var doc = new Document();
        doc.Info.Title = $"Wniosek #{data.ApplicationId}";

        // Marginesy i format strony
        var section = doc.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.TopMargin = "2cm";
        section.PageSetup.LeftMargin = "2cm";
        section.PageSetup.RightMargin = "2cm";

        // Styl bazowy
        var normal = doc.Styles["Normal"];
        normal.Font.Name = "Arial";
        normal.Font.Size = 10;

        // ----------------------
        // Stopka na każdej stronie
        // ----------------------
        var footerTable = section.Footers.Primary.AddTable();
        footerTable.AddColumn(Unit.FromCentimeter(16));
        var footerRow = footerTable.AddRow();
        footerRow.Shading.Color = Colors.LightGray;
        footerRow.Cells[0].AddParagraph($"Wygenerowano dnia: {data.DateNow:dd.MM.yyyy} poprzez aplikację SimplyManage")
            .Format.Alignment = ParagraphAlignment.Left;

        // ----------------------
        // Numer wniosku i data
        // ----------------------
        var topTable = section.AddTable();
        topTable.Borders.Width = 0.5;
        topTable.AddColumn("8cm");
        topTable.AddColumn("8cm");

        var topRow = topTable.AddRow();
        topRow.Cells[0].AddParagraph($"Numer wniosku\n{data.ApplicationId}");
        topRow.Cells[1].AddParagraph($"Data złożenia wniosku\n{data.ApplicationDate:dd.MM.yyyy}");

        section.AddParagraph().Format.SpaceAfter = "0.3cm";

        // ----------------------
        // Wnioskodawca
        // ----------------------
        AddPersonSection(section, "Wnioskodawca:", data.WhoFName, data.WhoLName, data.Degree, data.Department);

        // ----------------------
        // Wnioskuje dla
        // ----------------------
        AddPersonSection(section, "Wnioskuje dla:", data.ForFName, data.ForLName, data.DegreeFor, data.DepartmentFor);

        // ----------------------
        // Wiadomość
        // ----------------------
        var msgTable = section.AddTable();
        msgTable.Borders.Width = 0.5;
        msgTable.AddColumn(Unit.FromCentimeter(16));

        var msgRow1 = msgTable.AddRow();
        msgRow1.Cells[0].AddParagraph("Wiadomość dodatkowa do wniosku:");
        var msgRow2 = msgTable.AddRow();
        msgRow2.Cells[0].AddParagraph(data.MessageText ?? "");

        section.AddParagraph().Format.SpaceAfter = "0.3cm";

        // ----------------------
        // Data wygaśnięcia
        // ----------------------
        var expTable = section.AddTable();
        expTable.Borders.Width = 0.5;
        expTable.AddColumn(Unit.FromCentimeter(16));
        var expRow = expTable.AddRow();
        expRow.Cells[0].AddParagraph($"Data wygaśnięcia wniosku: {data.ExpireDate:dd.MM.yyyy}");

        section.AddParagraph().Format.SpaceAfter = "0.3cm";

        // ----------------------
        // Uprawnienia
        // ----------------------
        AddListTable(section, "Uprawnienia do programów / zasobów:", 
            new[] { "Program", "Uprawnienie", "Nazwa użytkownika" },
            data.Permissions.Select(p => new[] { p.Program, p.Permission, p.Login }).ToArray());

        section.AddParagraph().Format.SpaceAfter = "0.3cm";

        // ----------------------
        // Urządzenia
        // ----------------------
        AddListTable(section, "Urządzenia:", 
            new[] { "Nazwa urządzenia", "Numer seryjny" },
            data.Devices.Select(d => new[] { d.Name, d.Serial }).ToArray());

        section.AddParagraph().Format.SpaceAfter = "0.5cm";

        // ----------------------
        // Podpisy (tylko na końcu)
        // ----------------------
        var signTable = section.AddTable();
        signTable.AddColumn("5cm");
        signTable.AddColumn("5cm");
        signTable.AddColumn("5cm");

        var signRow = signTable.AddRow();
        signRow.Cells[0].AddParagraph($"Podpis administratora\n{data.AdmFName} {data.AdmLName}");
        signRow.Cells[1].AddParagraph($"Podpis wnioskującego\n{data.WhoFName} {data.WhoLName}");
        signRow.Cells[2].AddParagraph($"Podpis inspektora ochrony danych\n{data.IodFName} {data.IodLName}");

        // ----------------------
        // Render PDF
        // ----------------------
        var renderer = new PdfDocumentRenderer(true)
        {
            Document = doc
        };
        renderer.RenderDocument();

        using (var stream = new MemoryStream())
        {
            renderer.PdfDocument.Save(stream, false);
            return stream.ToArray();
        }
    }

    // Pomocnicze: Sekcja osoby
    private static void AddPersonSection(Section section, string title, string fname, string lname, string degree, string dept)
    {
        var table = section.AddTable();
        table.Borders.Width = 0.5;
        table.AddColumn("16cm");
        var head = table.AddRow();
        head.Shading.Color = Colors.LightGray;
        head.Cells[0].AddParagraph(title).Format.Font.Bold = true;

        var person = section.AddTable();
        person.Borders.Width = 0.5;
        person.AddColumn("8cm");
        person.AddColumn("8cm");
        var row1 = person.AddRow();
        row1.Cells[0].AddParagraph($"Imię i nazwisko\n{fname} {lname}");
        row1.Cells[1].AddParagraph($"Stanowisko\n{degree}");

        var row2 = person.AddRow();
        row2.Cells[0].AddParagraph("");
        row2.Cells[1].AddParagraph($"Wydział\n{dept}");

        section.AddParagraph().Format.SpaceAfter = "0.3cm";
    }

    // Pomocnicze: Tabela listowa z nagłówkiem i danymi
    private static void AddListTable(Section section, string title, string[] headers, string[][] rows)
    {
        var table = section.AddTable();
        table.Borders.Width = 0.5;
        foreach (var _ in headers)
            table.AddColumn(Unit.FromCentimeter(16.0 / headers.Length));

        var headRow1 = table.AddRow();
        headRow1.Shading.Color = Colors.LightGray;
        headRow1.Cells[0].MergeRight = headers.Length - 1;
        headRow1.Cells[0].AddParagraph(title).Format.Font.Bold = true;

        var headerRow = table.AddRow();
        headerRow.HeadingFormat = true; // powtarza na każdej stronie
        for (int i = 0; i < headers.Length; i++)
            headerRow.Cells[i].AddParagraph(headers[i]).Format.Font.Bold = true;

        foreach (var r in rows)
        {
            var row = table.AddRow();
            for (int i = 0; i < headers.Length; i++)
                row.Cells[i].AddParagraph(r[i] ?? "");
        }
    }
}*/