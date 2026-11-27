using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BichonDesktopPet.App.Services;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;

namespace BichonDesktopPet.App.Views;

public partial class PersonaEditorWindow : Window
{
    private readonly AvatarCatalogService _catalog;
    private readonly AvatarProfile _avatar;

    public PersonaEditorWindow(AvatarCatalogService catalog, AvatarProfile avatar)
    {
        InitializeComponent();

        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _avatar = avatar ?? throw new ArgumentNullException(nameof(avatar));

        LoadProfile();
    }

    private void LoadProfile()
    {
        var persona = PersonaProfileValidator.NormalizeOrDefault(_avatar.Persona);

        NameTextBox.Text = _avatar.Name;
        SpeciesComboBox.SelectedValue = _avatar.Species;
        BreedTextBox.Text = _avatar.Breed;
        NicknamesTextBox.Text = string.Join("，", persona.Nicknames);
        SelfReferenceTextBox.Text = persona.SelfReference;
        UserAddressTextBox.Text = persona.UserAddress;
        TraitsTextBox.Text = string.Join("，", persona.PersonalityTraits);
        SpeechStyleComboBox.SelectedValue = persona.SpeechStyle;
        ResponseLengthComboBox.SelectedValue = persona.ResponseLength;
        EmojiLevelComboBox.SelectedValue = persona.EmojiLevel;
        CatchphrasesTextBox.Text = string.Join(Environment.NewLine, persona.Catchphrases);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var persona = new PersonaProfile
        {
            Nicknames = SplitValues(NicknamesTextBox.Text, ',', '，', ';', '；', '\r', '\n'),
            SelfReference = SelfReferenceTextBox.Text,
            UserAddress = UserAddressTextBox.Text,
            PersonalityTraits = SplitValues(TraitsTextBox.Text, ',', '，', ';', '；', '\r', '\n'),
            SpeechStyle = SelectedTag(SpeechStyleComboBox, "cute"),
            ResponseLength = SelectedTag(ResponseLengthComboBox, "short"),
            EmojiLevel = SelectedTag(EmojiLevelComboBox, "medium"),
            Catchphrases = SplitValues(CatchphrasesTextBox.Text, '\r', '\n')
        };

        try
        {
            _catalog.UpdateProfile(
                _avatar.Id,
                NameTextBox.Text,
                SelectedTag(SpeciesComboBox, "custom"),
                BreedTextBox.Text,
                persona);

            DialogResult = true;
        }
        catch (ArgumentException exception)
        {
            StatusText.Text = exception.Message;
        }
        catch (Exception exception)
        {
            AppLogger.Log($"Unable to save persona for avatar '{_avatar.Id}'.", exception);
            StatusText.Text = "保存失败，请检查本地日志。";
        }
    }

    private static string SelectedTag(ComboBox comboBox, string fallback) =>
        comboBox.SelectedValue?.ToString() ?? fallback;

    private static List<string> SplitValues(string? value, params char[] separators) =>
        (value ?? string.Empty)
            .Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
