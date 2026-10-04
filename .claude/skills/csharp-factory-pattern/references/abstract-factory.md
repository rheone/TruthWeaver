# Abstract Factory

Abstract Factory creates a *family* of related objects through one interface, guaranteeing that
whatever concrete family is selected, every object it produces is mutually compatible — a consumer
never accidentally mixes a component from one family with a component from another.

## Shape

```csharp
public interface IUiComponentFactory
{
    IButton CreateButton();
    ICheckbox CreateCheckbox();
    IScrollbar CreateScrollbar();
}

public sealed class DarkThemeComponentFactory : IUiComponentFactory
{
    public IButton CreateButton() => new DarkButton();
    public ICheckbox CreateCheckbox() => new DarkCheckbox();
    public IScrollbar CreateScrollbar() => new DarkScrollbar();
}

public sealed class LightThemeComponentFactory : IUiComponentFactory
{
    public IButton CreateButton() => new LightButton();
    public ICheckbox CreateCheckbox() => new LightCheckbox();
    public IScrollbar CreateScrollbar() => new LightScrollbar();
}
```

A consumer depends on `IUiComponentFactory`, never on which concrete theme is active:

```csharp
public sealed class SettingsDialog
{
    private readonly IButton _saveButton;
    private readonly ICheckbox _rememberMeCheckbox;
    private readonly IScrollbar _scrollbar;

    public SettingsDialog(IUiComponentFactory factory)
    {
        _saveButton = factory.CreateButton();
        _rememberMeCheckbox = factory.CreateCheckbox();
        _scrollbar = factory.CreateScrollbar();
    }
}
```

Whichever concrete factory `SettingsDialog` receives, its button, checkbox, and scrollbar are
guaranteed to come from the same family — there is no code path by which `SettingsDialog` ends up
with a `DarkButton` next to a `LightCheckbox`, because the factory that produced one produced the
other. This is Abstract Factory's actual guarantee: consistency *across* the produced objects, not
merely creation-point centralization for a single type, which Factory Method already provides on its
own.

## Abstract Factory vs. several separate Factory Methods

Passing `SettingsDialog` three separate factories — `IButtonFactory`, `ICheckboxFactory`,
`IScrollbarFactory` — would centralize each type's creation individually, but nothing would prevent
constructing `SettingsDialog` with a `DarkButtonFactory` alongside a `LightCheckboxFactory` by
mistake; each factory is independently satisfiable, so the compiler can't catch a mismatched
combination. Abstract Factory's single interface producing the whole family is what makes a
mismatched combination structurally impossible rather than merely a discipline the caller has to
maintain by hand. Reach for Abstract Factory specifically when "these objects must come from the
same family together" is a real invariant worth protecting — not merely because more than one type
needs a factory.

## Adding a new product to the family

Adding a new product kind (`IMenu`) to every family means adding `CreateMenu()` to
`IUiComponentFactory` and implementing it in every existing concrete factory
(`DarkThemeComponentFactory`, `LightThemeComponentFactory`) — the same "every implementation must be
updated" cost the Visitor pattern's interface has for a new visited type, and for the same reason:
the interface is the contract every family must uphold completely. This is the direction of change
Abstract Factory makes expensive; adding a new *family* (a new theme) is the direction it makes
cheap — see the extension reference for the full breakdown of both directions.
