using Godot;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Com.IsartDigital.Sokoban;

public partial class Login : Screen
{
    [ExportCategory("Login Validation")]
    private string allowedCharactersRegex = @"^[A-Za-z0-9]+$";
    [Export] private int minCharactersRequired = 3;
    private const string ERR_TOO_SHORT_NAME = "ID_TOO_SHORT_NAME";
    private const string ERR_TOO_SHORT_PASSWORLD = "ID_TOO_SHORT_PASSWORD";
    private const string ERR_INVALID_CHAR = "ID_INVALID_CHAR";

    [ExportCategory("Animation Parameters")]
    [Export] private Vector2 movementOfLineEdit = new Vector2(100, 0);
    [Export] private Vector2 nameBoxOffset = new Vector2(0, 52);
    [Export] private float errorAnimDuration = 0.05f;

    [ExportCategory("UI References")]
    [Export] private Label title;
    [Export] private VBoxContainer nameBox;
    [Export] private VBoxContainer passwordBox;
    [Export] private LineEdit loginLineEdit;
    [Export] private LineEdit passwordLineEdit;
    [Export] private Label warningText;
    [Export] private Label warningTextForPassword;
    [Export] private TextureButton seeingPasswordButton;
    [Export] private TextureButton confirmButton;
    [Export] private Button guestButton;

    private Vector2 startPosPassword;
    private Vector2 startPosName;

    private bool playerAsEnterValidePseudo = false;
    private bool playerAsEnterValidePassword = false;
    private bool onNameLineEdit = false;
    private bool onPasswordLineEdit = false;

    private string loginName;
    private string hashedLoginPassword;

    private static Login instance;
    public static Login GetInstance() => instance;

    public override void _Ready()
    {
        base._Ready();

        if (instance == null)
            instance = this;
        else
        {
            QueueFree();
            return;
        }

        confirmButton.Visible = false;
        loginLineEdit.Visible = false;
        passwordLineEdit.Visible = false;

        onNameLineEdit = true;

        loginLineEdit.FocusEntered += OnNameLinePressed;
        loginLineEdit.FocusExited += OnNameLineExited;

        passwordLineEdit.FocusEntered += OnPasswordLinePressed;
        passwordLineEdit.FocusExited += OnPasswordLineExited;

        loginLineEdit.TextChanged += OnLoginTextChanged;
        passwordLineEdit.TextChanged += OnPasswordTextChanged;

        seeingPasswordButton.Pressed += OnPasswordSeeingButtonPressed;
        guestButton.Pressed += OnGuestButtonPressed;
        confirmButton.Pressed += OnConfirmButtonPressed;

        if (title != null)
        {
            title.PivotOffset = title.Size / 2;
        }
    }

    private void OnNameLineExited() => onNameLineEdit = false;
    private void OnNameLinePressed() => onNameLineEdit = true;
    private void OnPasswordLineExited() => onPasswordLineEdit = false;
    private void OnPasswordLinePressed() => onPasswordLineEdit = true;

    private void OnLoginTextChanged(string pNewText)
    {
        if (!playerAsEnterValidePseudo)
            CleanInput(loginLineEdit, warningText);
    }

    private void OnPasswordTextChanged(string pNewText)
    {
        if (!playerAsEnterValidePassword)
            CleanInput(passwordLineEdit, warningTextForPassword);
    }

    private void OnPasswordSeeingButtonPressed()
    {
        passwordLineEdit.Secret = !passwordLineEdit.Secret;
    }

    private void OnGuestButtonPressed()
    {
        loginName = "Guest";
        hashedLoginPassword = HashPassword("Guest");

        if (Saving.GetInstance() == null) return;

        Saving.GetInstance().CheckNameInDataBase(loginName, hashedLoginPassword);
        MenuManager.GetInstance().ChangeMenu(this, (int)MenuList.TITLE);
    }

    private void OnConfirmButtonPressed()
    {
        if (!playerAsEnterValidePseudo)
        {
            if (loginLineEdit.Text.Length >= minCharactersRequired)
            {
                loginName = loginLineEdit.Text;
                playerAsEnterValidePseudo = true;
                loginLineEdit.Editable = false;
                warningText.Visible = false;
            }
            else
            {
                warningText.Text = string.Format(ERR_TOO_SHORT_NAME, minCharactersRequired);
                warningText.Visible = true;
                ErrorAnime(loginLineEdit, startPosName);
            }
        }

        if (!playerAsEnterValidePassword)
        {
            if (passwordLineEdit.Text.Length >= minCharactersRequired)
            {
                hashedLoginPassword = HashPassword(passwordLineEdit.Text);
                playerAsEnterValidePassword = true;
                passwordLineEdit.Editable = false;
                warningTextForPassword.Visible = false;
            }
            else
            {
                warningTextForPassword.Text = string.Format(ERR_TOO_SHORT_PASSWORLD, minCharactersRequired);
                warningTextForPassword.Visible = true;
                ErrorAnime(passwordLineEdit, startPosPassword);
            }
        }

        if (playerAsEnterValidePassword && playerAsEnterValidePseudo)
        {
            if (Saving.GetInstance() == null) return;

            Saving.GetInstance().CheckNameInDataBase(loginName, hashedLoginPassword);
        }
    }

    public override void _Input(InputEvent pEvent)
    {
        if (pEvent is InputEventKey lKeyEvent)
        {
            if (lKeyEvent.Pressed && lKeyEvent.Keycode == Key.Enter)
            {
                if (onNameLineEdit)
                    passwordLineEdit.GrabFocus();
                else
                    OnConfirmButtonPressed();
            }
        }
    }

    private void CleanInput(LineEdit pLineEdit, Label pWarningLabel)
    {
        if (string.IsNullOrEmpty(pLineEdit.Text)) return;

        if (!Regex.IsMatch(pLineEdit.Text, allowedCharactersRegex))
        {
            foreach (char lCaract in pLineEdit.Text)
            {
                string sChar = lCaract.ToString();

                if (!Regex.IsMatch(sChar, allowedCharactersRegex))
                {
                    pLineEdit.Text = pLineEdit.Text.Replace(sChar, "");
                    pLineEdit.CaretColumn = pLineEdit.Text.Length;
                    pWarningLabel.Text = string.Format(ERR_INVALID_CHAR, sChar);
                    pWarningLabel.Visible = true;
                }
            }
        }
    }

    private string HashPassword(string pRawPassword)
    {
        byte[] lInputBytes = Encoding.UTF8.GetBytes(pRawPassword);
        return Convert.ToBase64String(lInputBytes);
    }

    public bool VerifyPassword(string pInputPassword, string pStoredHash)
    {
        return HashPassword(pInputPassword) == pStoredHash;
    }

    public void Switch()
    {
        MenuManager.GetInstance().ChangeMenu(this, (int)MenuList.TITLE);
        TitleScreen.SetUserName();
    }

    public void ResetLogin()
    {
        passwordLineEdit.Text = "";
        passwordLineEdit.Editable = true;
        loginLineEdit.Text = "";
        loginLineEdit.Editable = true;
        playerAsEnterValidePassword = false;
        playerAsEnterValidePseudo = false;
        warningText.Visible = false;
        warningTextForPassword.Visible = false;
    }

    private void ErrorAnime(LineEdit lLineEdit, Vector2 lStartPos)
    {
        lLineEdit.Position = lStartPos;
        Tween lTween = CreateTween();
        lTween.SetTrans(Tween.TransitionType.Quint).SetEase(Tween.EaseType.Out);

        lTween.TweenProperty(lLineEdit, (string)Control.PropertyName.Position, lLineEdit.Position - movementOfLineEdit, errorAnimDuration);
        lTween.TweenProperty(lLineEdit, (string)Control.PropertyName.Position, lStartPos, errorAnimDuration);
        lTween.TweenProperty(lLineEdit, (string)Control.PropertyName.Position, lLineEdit.Position + movementOfLineEdit, errorAnimDuration);
        lTween.TweenProperty(lLineEdit, (string)Control.PropertyName.Position, lStartPos, errorAnimDuration);
    }

    public override void Load()
    {
        base.Load();

        loginLineEdit.Visible = true;
        passwordLineEdit.Visible = true;
        confirmButton.Visible = true;

        startPosName = loginLineEdit.Position + nameBoxOffset;
        startPosPassword = passwordLineEdit.Position;

        Color invisibleColor = new Color(1, 1, 1, 0);
        Color visibleColor = new Color(1, 1, 1, 1);

        nameBox.Modulate = invisibleColor;
        passwordBox.Modulate = invisibleColor;
        confirmButton.Modulate = invisibleColor;

        if (guestButton != null) guestButton.Modulate = invisibleColor;
        if (seeingPasswordButton != null) seeingPasswordButton.Modulate = invisibleColor;

        float lTitleTweenDuration = 2f;
        float lElementsStartDuration = 0.7f;
        float lElementDelay = 0.2f;
        float lElementFadeDuration = 0.5f;

        Tween lTween = CreateTween();
        lTween.SetParallel(true);

        if (title != null)
        {
            title.Scale = new Vector2(2.5f, 2.5f);
            lTween.TweenProperty(title, (string)Control.PropertyName.Scale, Vector2.One, lTitleTweenDuration)
                  .SetTrans(Tween.TransitionType.Elastic)
                  .SetEase(Tween.EaseType.Out);
        }

        lTween.TweenProperty(nameBox, (string)CanvasItem.PropertyName.Modulate, visibleColor, lElementFadeDuration)
              .SetDelay(lElementsStartDuration);

        lTween.TweenProperty(passwordBox, (string)CanvasItem.PropertyName.Modulate, visibleColor, lElementFadeDuration)
              .SetDelay(lElementsStartDuration + lElementDelay);

        if (seeingPasswordButton != null)
        {
            lTween.TweenProperty(seeingPasswordButton, (string)CanvasItem.PropertyName.Modulate, visibleColor, lElementFadeDuration)
                  .SetDelay(lElementsStartDuration + lElementDelay);
        }

        lTween.TweenProperty(confirmButton, (string)CanvasItem.PropertyName.Modulate, visibleColor, lElementFadeDuration)
              .SetDelay(lElementsStartDuration + (lElementDelay * 2));

        if (guestButton != null)
        {
            lTween.TweenProperty(guestButton, (string)CanvasItem.PropertyName.Modulate, visibleColor, lElementFadeDuration)
                  .SetDelay(lElementsStartDuration + (lElementDelay * 3));
        }
    }

    protected override void Dispose(bool pDisposing)
    {
        instance = null;
        base.Dispose(pDisposing);
    }
}
