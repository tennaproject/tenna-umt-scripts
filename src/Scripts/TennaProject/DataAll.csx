#r "System.Windows.Forms"

using System;
using System.IO;
using System.Windows.Forms;

string[] exportLabels = new string[] {
  "Consumables",
  "Weapons",
  "Armors",
  "Key Items",
  "Light World Items",
  "Rooms",
  "Spells",
  "Enemies",
};

string[] exportFiles = new string[] {
  "DataExportConsumables.csx",
  "DataExportWeapons.csx",
  "DataExportArmors.csx",
  "DataExportKeyItems.csx",
  "DataExportLightWorldItems.csx",
  "DataExportRooms.csx",
  "DataExportSpells.csx",
  "DataExportEnemies.csx",
};

bool[] defaultSelected = new bool[] {
  true,
  true,
  true,
  true,
  true,
  true,
  true,
  true,
};

bool[] selected = TennaSelectCheckedItems("Tenna Data Export", "Choose data categories to export", exportLabels, defaultSelected);
if (selected == null)
{
  return;
}

int selectedCount = 0;
for (int i = 0; i < selected.Length; i++)
{
  if (selected[i])
    selectedCount++;
}

if (selectedCount == 0)
{
  ScriptMessage("No data categories selected.");
  return;
}

string scriptDir = Path.GetDirectoryName(ScriptPath);
if (string.IsNullOrWhiteSpace(scriptDir))
{
  ScriptError("Could not resolve script folder from ScriptPath.");
  return;
}

string exportDir = TennaSelectExportDir();
int exportedCount = 0;

try
{
  Environment.SetEnvironmentVariable("TENNA_UMT_EXPORT_DIR", exportDir);

  for (int i = 0; i < selected.Length; i++)
  {
    if (!selected[i])
      continue;

    string path = Path.Join(scriptDir, exportFiles[i]);
    if (!File.Exists(path))
    {
      ScriptError("Missing selected exporter:\n" + path);
      return;
    }

    if (!RunUMTScript(path))
    {
      ScriptError("Stopped after " + exportLabels[i] + " failed.");
      return;
    }

    exportedCount++;
  }
}
finally
{
  Environment.SetEnvironmentVariable("TENNA_UMT_EXPORT_DIR", null);
}

ScriptMessage("Data export complete. Exported " + exportedCount + " selected categor" + (exportedCount == 1 ? "y" : "ies") + ".\n" + exportDir);

string TennaSelectExportDir()
{
  try
  {
    using (FolderBrowserDialog dialog = new FolderBrowserDialog())
    {
      dialog.Description = "Choose export folder";
      dialog.ShowNewFolderButton = true;

      if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
        return dialog.SelectedPath;
    }
  }
  catch
  {
  }

  ScriptError("Export cancelled. No folder selected.");
  throw new Exception("Export cancelled. No folder selected.");
}

bool[] TennaSelectCheckedItems(string title, string prompt, string[] names, bool[] defaultSelected)
{
  using (Form form = new Form())
  using (Label label = new Label())
  using (CheckedListBox list = new CheckedListBox())
  using (Button ok = new Button())
  using (Button cancel = new Button())
  {
    form.Text = title;
    form.Width = 380;
    form.Height = 360;
    form.StartPosition = FormStartPosition.CenterScreen;
    form.FormBorderStyle = FormBorderStyle.FixedDialog;
    form.MaximizeBox = false;
    form.MinimizeBox = false;

    label.Text = prompt;
    label.Left = 12;
    label.Top = 12;
    label.Width = 340;
    label.Height = 22;
    form.Controls.Add(label);

    list.Left = 12;
    list.Top = 42;
    list.Width = 340;
    list.Height = 230;
    list.CheckOnClick = true;
    for (int i = 0; i < names.Length; i++)
      list.Items.Add(names[i], defaultSelected[i]);
    form.Controls.Add(list);

    ok.Text = "Export Selected";
    ok.Left = 125;
    ok.Top = 282;
    ok.Width = 110;
    ok.DialogResult = DialogResult.OK;
    form.Controls.Add(ok);

    cancel.Text = "Cancel";
    cancel.Left = 245;
    cancel.Top = 282;
    cancel.Width = 80;
    cancel.DialogResult = DialogResult.Cancel;
    form.Controls.Add(cancel);

    form.AcceptButton = ok;
    form.CancelButton = cancel;

    if (form.ShowDialog() != DialogResult.OK)
      return null;

    bool[] result = new bool[names.Length];
    for (int i = 0; i < names.Length; i++)
      result[i] = list.GetItemChecked(i);
    return result;
  }
}
