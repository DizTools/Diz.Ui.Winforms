using System.Globalization;
using Diz.Core.model.snes;
using Diz.Core.util;

namespace Diz.Ui.Winforms.dialogs;

public partial class GotoDialog : Form
{
    private readonly Data data;
    public GotoDialog(int offset, Data data)
    {
        InitializeComponent();
        this.data = data;
        textROM.Text = Util.NumberToBaseString(data.ConvertPCtoSnes(offset), Util.NumberBase.Hexadecimal, 6);
        textPC.Text = Util.NumberToBaseString(offset, Util.NumberBase.Hexadecimal, 0);
    }

    private void GotoDialog_Load(object sender, EventArgs e)
    {
        textROM.SelectAll();
        UpdateUi();
    }

    private int ParseOffset(string text)
    {
        var style = radioDec.Checked ? NumberStyles.Number : NumberStyles.HexNumber;
        return int.TryParse(text, style, null, out var offset) ? offset : -1;
    }

    public int GetPcOffset() => ParseOffset(textPC.Text);
        

    private bool currentlyUpdatingText;
    private bool TryLockText()
    {
        if (currentlyUpdatingText) 
            return false;
            
        currentlyUpdatingText = true;
        return true;
    }
    private void UnlockText() => currentlyUpdatingText = false;

    private void UpdateTextChanged(string txtChanged, Action<string, int, Util.NumberBase> onSuccess)
    {
        // don't allow UI callbacks to mess up what we're doing, lock further calls to this function til we're done
        if (!TryLockText()) 
            return;

        UpdateTextChangedInternal(txtChanged, !radioDec.Checked, onSuccess);

        UnlockText();
    }

    private static void UpdateTextChangedInternal(string txtChanged, bool formatAsHex, Action<string, int, Util.NumberBase> onSuccess)
    {
        var style = formatAsHex ? NumberStyles.HexNumber : NumberStyles.Number;

        if (!ByteUtil.TryParseNum_Stripped(ref txtChanged, style, out var address) || address < 0) 
            return;

        onSuccess(
            txtChanged, 
            address, 
            formatAsHex ? Util.NumberBase.Hexadecimal : Util.NumberBase.Decimal
        );
    }

    // For both textbox TextChanged events:
    // precondition: unvalidated input in textbox
    // postcondtion: valid text is in both textboxes, or, button is greyed out and error message displayed.

    private void UpdateUi()
    {
        var valid = true;
        lblError.Text = "";

        if (!IsPcOffsetValid())
        {
            lblError.Text = "Invalid ROM File Offset";
            valid = false;
        }

        if (!IsRomAddressValid())
        {
            lblError.Text = "Invalid SNES Address";
            valid = false;
        }

        go.Enabled = valid;
    }

    private bool IsValidPcAddress(int pc) => 
        pc >= 0 && pc < data.GetRomSize();

    private bool IsPcOffsetValid() => 
        IsValidPcAddress(GetPcOffset());

    private bool IsRomAddressValid()
    {
        var address = ParseOffset(textROM.Text);
        return address >= 0 && IsValidPcAddress(data.ConvertSnesToPc(address));
    }

    private void textROM_TextChanged(object sender, EventArgs e)
    {
        UpdateTextChanged(textROM.Text,(finalText, address, noBase) =>
        {
            var pc = data.ConvertSnesToPc(address);
                
            textROM.Text = finalText;
            textPC.Text = Util.NumberToBaseString(pc, noBase, 0);
        });

        UpdateUi();
    }

    private void textPC_TextChanged(object sender, EventArgs e)
    {
        UpdateTextChanged(textPC.Text, (finalText, offset, noBase) =>
        {
            var addr = data.ConvertPCtoSnes(offset);

            textPC.Text = finalText;
            textROM.Text = Util.NumberToBaseString(addr, noBase, 6);
        });

        UpdateUi();
    }
        
    private void OnTextKeydown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && textROM.Text.Length > 0) 
            Finish();
    }
        
    private void Finish() => DialogResult = DialogResult.OK;

    private void go_Click(object sender, EventArgs e) => Finish();

    private void textROM_KeyDown(object sender, KeyEventArgs e) => OnTextKeydown(e);

    private void textPC_KeyDown(object sender, KeyEventArgs e) => OnTextKeydown(e);
        
    private void cancel_Click(object sender, EventArgs e) => Close();

    private void radioHex_CheckedChanged(object sender, EventArgs e)
    {
        if (radioHex.Checked) {
            if (int.TryParse(textPC.Text, out var result))
            {
                textPC.Text = Util.NumberToBaseString(result, Util.NumberBase.Hexadecimal, 0);
            }
        } else {
            if (int.TryParse(textPC.Text, NumberStyles.HexNumber, null, out var result))
            {
                textPC.Text = result.ToString();
            }
        }
    }
}