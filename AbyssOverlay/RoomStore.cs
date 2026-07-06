using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using ExcelDataReader;

namespace AbyssOverlay;

public sealed class RoomStore
{
    private readonly string _xlsxPath;
    private readonly Dictionary<string, RoomInfo> _rooms = new(StringComparer.OrdinalIgnoreCase);

    public RoomStore(string xlsxPath)
    {
        _xlsxPath = xlsxPath;
        Reload();
    }

    public IReadOnlyDictionary<string, RoomInfo> Rooms => _rooms;

    public string ExcelPath => _xlsxPath;

    public void Reload()
    {
        _rooms.Clear();

        if (!File.Exists(_xlsxPath))
        {
            return;
        }

        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        using var stream = File.Open(_xlsxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var ds = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
        });

        if (ds.Tables.Count == 0)
        {
            return;
        }

        var table = ds.Tables[0];
        var cols = table.Columns.Cast<DataColumn>().ToDictionary(c => c.ColumnName.Trim(), c => c.Ordinal, StringComparer.OrdinalIgnoreCase);

        int Col(string name) => cols.TryGetValue(name, out var idx) ? idx : -1;

        var iRoom = Col("Room");
        var iTarget = Col("Target");
        var iNum = Col("#");
        var iRole = Col("Role");
        var iDetails = Col("Details (RU)");
        if (iDetails < 0) iDetails = Col("Details");
        var iNotes = Col("Notes (RU)");
        if (iNotes < 0) iNotes = Col("Notes");

        if (iRoom < 0 || iTarget < 0)
        {
            return;
        }

        string lastRoom = string.Empty;

        foreach (DataRow row in table.Rows)
        {
            var roomVal = iRoom >= 0 ? row[iRoom]?.ToString() : string.Empty;
            if (!string.IsNullOrWhiteSpace(roomVal))
            {
                lastRoom = roomVal.Trim();
            }

            if (string.IsNullOrWhiteSpace(lastRoom))
            {
                continue;
            }

            if (!_rooms.TryGetValue(lastRoom, out var room))
            {
                room = new RoomInfo { Name = lastRoom };
                _rooms[lastRoom] = room;
            }

            var noteVal = iNotes >= 0 ? row[iNotes]?.ToString() : string.Empty;
            if (!string.IsNullOrWhiteSpace(noteVal))
            {
                var n = noteVal.Trim();
                if (!room.Notes.Contains(n))
                {
                    room.Notes.Add(n);
                }
            }

            var targetVal = iTarget >= 0 ? row[iTarget]?.ToString() : string.Empty;
            if (string.IsNullOrWhiteSpace(targetVal))
            {
                continue;
            }

            var target = targetVal.Trim();
            var num = iNum >= 0 ? row[iNum]?.ToString() ?? string.Empty : string.Empty;
            var role = iRole >= 0 ? row[iRole]?.ToString() ?? string.Empty : string.Empty;
            var details = iDetails >= 0 ? row[iDetails]?.ToString() ?? string.Empty : string.Empty;

            if (!room.Targets.Any(t => Similarity.Normalize(t) == Similarity.Normalize(target)))
            {
                room.Targets.Add(target);
            }

            room.Priority.Add(new PriorityItem
            {
                Num = num.Trim(),
                Target = target,
                Role = role.Trim(),
                Details = details.Trim()
            });
        }
    }
}
