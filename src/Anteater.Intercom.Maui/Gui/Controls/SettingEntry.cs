namespace Anteater.Intercom.Gui.Controls;

using Sharp.UI;

public class SettingEntry : Entry
{
    public static readonly BindableProperty KeyProperty =
        BindableProperty.Create(nameof(Key), typeof(string), typeof(SettingEntry), null);

    public static readonly BindableProperty DefaultProperty =
        BindableProperty.Create(nameof(Default), typeof(string), typeof(SettingEntry), null);

    public string Key
    {
        get => GetValue(KeyProperty) as string;
        set => SetValue(KeyProperty, value);
    }

    public string Default
    {
        get => GetValue(DefaultProperty) as string;
        set => SetValue(DefaultProperty, value);
    }
}

public static class SettingsEntryExtensions
{
    public static SettingEntry Key(this SettingEntry entry, string value)
    {
        entry.Key = value;
        return entry;
    }

    public static SettingEntry Default(this SettingEntry entry, string value)
    {
        entry.Default = value;
        return entry;
    }
}

