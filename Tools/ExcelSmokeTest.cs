using System;
using System.IO;
using OfficeOpenXml;
using AudienciasApp.Services;
using AudienciasApp.Models;
using System.Threading.Tasks;

class ExcelSmokeTest
{
    static async Task<int> Main()
    {
        Console.WriteLine("Running Excel smoke test...");

        var service = new ExcelService();
        var created = await service.CreateNewFileAsync();
        Console.WriteLine($"Created: {created}");

        // Create a SI hearing
        var h1 = new Hearing
        {
            CaseCode = "RAD-001",
            HearingType = "Audiencia concentrada",
            Date = DateTime.Today,
            Time = "09:00",
            Court = "Juzgado 1",
            WasHeld = true,
            Observations = "OK"
        };

        await service.SaveHearingAsync(h1);

        // Create a NO hearing with reason Juez
        var h2 = new Hearing
        {
            CaseCode = "RAD-002",
            HearingType = "Audiencia pública",
            Date = DateTime.Today,
            Time = "10:00",
            Court = "Juzgado 2",
            WasHeld = false,
            ReasonNotHeld = "Juez",
            Observations = "No estuvo"
        };

        await service.SaveHearingAsync(h2);

        // Now open the file and read totals
        var createdPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ArchivosCreados", created);
        if (!File.Exists(createdPath))
        {
            Console.WriteLine("Created file not found at " + createdPath);
            return 2;
        }

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using (var pkg = new ExcelPackage(new FileInfo(createdPath)))
        {
            var ws = pkg.Workbook.Worksheets[0];
            Console.WriteLine($"G111: {ws.Cells["G111"].Text}");
            Console.WriteLine($"H111: {ws.Cells["H111"].Text}");
            Console.WriteLine($"Q111: {ws.Cells["Q111"].Text}");
            Console.WriteLine($"I111 (Juez): {ws.Cells["I111"].Text}");
        }

        Console.WriteLine("Smoke test completed.");
        return 0;
    }
}
