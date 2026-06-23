#r "System.Windows.Forms"
#r "System.Drawing"

using System;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

string[] scriptLabels = new string[] {
  "Core (required)",
  "Flag Watcher",
  "Plot Watcher",
  "Flag Editor",
  "State Dump",
  "Notes",
  "Save Manager",
};

string[] scriptFiles = new string[] {
  "GameFlagWatcher.csx",
  "GamePlotWatcher.csx",
  "GameFlagEditor.csx",
  "GameStateDump.csx",
  "GameNotes.csx",
  "GameSaveManager.csx",
};

bool[] selected = TennaSelectCheckedItems("Tenna Game Scripts", "Choose game scripts to install", scriptLabels);
if (selected == null)
{
  return;
}

string scriptDir = Path.GetDirectoryName(ScriptPath);
if (string.IsNullOrWhiteSpace(scriptDir))
{
  ScriptError("Could not resolve script folder from ScriptPath.");
  return;
}

int selectedCount = 0;
int installedCount = 0;

try
{
  Environment.SetEnvironmentVariable("TENNA_UMT_SUPPRESS_SCRIPT_MESSAGES", "1");
  SetFinishedMessage(false);

  string corePath = Path.Join(scriptDir, "GameCore.csx");
  if (!File.Exists(corePath))
  {
    ScriptError("Missing mandatory Core script:\n" + corePath);
    return;
  }

  if (!TennaIsCoreInstalled())
  {
    if (!TennaRunQuiet(corePath))
    {
      ScriptError("Stopped after mandatory Core failed.");
      return;
    }

    installedCount++;
  }

  for (int i = 1; i < selected.Length; i++)
  {
    if (!selected[i])
      continue;

    selectedCount++;
    int scriptIndex = i - 1;
    string path = Path.Join(scriptDir, scriptFiles[scriptIndex]);
    if (!File.Exists(path))
    {
      ScriptError("Missing selected script:\n" + path);
      return;
    }

    if (!TennaRunQuiet(path))
    {
      ScriptError("Stopped after " + scriptLabels[i] + " failed.");
      return;
    }

    installedCount++;
  }
}
finally
{
  Environment.SetEnvironmentVariable("TENNA_UMT_SUPPRESS_SCRIPT_MESSAGES", null);
  SetFinishedMessage(true);
}

if (selectedCount == 0)
{
  ScriptMessage("Game script install complete. Core is installed. No optional game scripts selected.");
  return;
}

ScriptMessage("Game script install complete. Core is installed. Applied " + installedCount + " script(s).");

bool TennaRunQuiet(string path)
{
  SetFinishedMessage(false);
  return RunUMTScript(path);
}

bool TennaIsCoreInstalled()
{
  EnsureDataLoaded();

  if (Data.Code.ByName("gml_Object_obj_time_Create_0") is not UndertaleCode createCode)
    return false;

  return GetDecompiledText(createCode).Contains("_tenna_core_enabled");
}

bool[] TennaSelectCheckedItems(string title, string prompt, string[] names)
{
  using (Form form = new Form())
  using (Label label = new Label())
  using (CheckedListBox list = new CheckedListBox())
  using (Button ok = new Button())
  using (Button cancel = new Button())
  {
    form.Text = title;
    form.Width = 360;
    form.Height = 330;
    form.StartPosition = FormStartPosition.CenterScreen;
    form.FormBorderStyle = FormBorderStyle.FixedDialog;
    form.MaximizeBox = false;
    form.MinimizeBox = false;

    label.Text = prompt;
    label.Left = 12;
    label.Top = 12;
    label.Width = 320;
    label.Height = 22;
    form.Controls.Add(label);

    list.Left = 12;
    list.Top = 42;
    list.Width = 320;
    list.Height = 200;
    list.CheckOnClick = true;
    list.DrawMode = DrawMode.OwnerDrawFixed;
    list.ItemCheck += (sender, args) =>
    {
      if (args.Index == 0)
        args.NewValue = CheckState.Checked;
    };
    list.DrawItem += (sender, args) =>
    {
      if (args.Index < 0)
        return;

      args.DrawBackground();
      bool isCore = args.Index == 0;
      CheckBoxState state = list.GetItemChecked(args.Index) ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal;
      CheckBoxRenderer.DrawCheckBox(args.Graphics, new System.Drawing.Point(args.Bounds.Left + 1, args.Bounds.Top + 1), state);

      using (System.Drawing.Brush brush = new System.Drawing.SolidBrush(isCore ? System.Drawing.SystemColors.GrayText : args.ForeColor))
      {
        args.Graphics.DrawString(list.Items[args.Index].ToString(), args.Font, brush, args.Bounds.Left + 22, args.Bounds.Top + 1);
      }

      args.DrawFocusRectangle();
    };
    for (int i = 0; i < names.Length; i++)
      list.Items.Add(names[i], true);
    form.Controls.Add(list);

    ok.Text = "Install Selected";
    ok.Left = 110;
    ok.Top = 252;
    ok.Width = 105;
    ok.DialogResult = DialogResult.OK;
    form.Controls.Add(ok);

    cancel.Text = "Cancel";
    cancel.Left = 225;
    cancel.Top = 252;
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
