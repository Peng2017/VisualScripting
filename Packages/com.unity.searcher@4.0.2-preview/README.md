# Searcher

## Features

![GitHub Logo](/Documentation~/images/tree_view.png) ![GitHub Logo](/Documentation~/images/quick_search.png)
* Popup Window Placement
* Tree View
* Keyboard Navigation
* Quick Search
* Auto-Complete
* Match Highlighting
* Multiple Databases

## Quick Usage Example

```csharp
void OnMouseDown( MouseDownEvent evt )
{
    var items = new List<SearcherItem>
    {
        new SearcherItem( "Books", "Description", new List<SearcherItem>()
        {
            new SearcherItem( "Dune" ),
        } )
    };
    items[0].AddChild( new SearcherItem( "Ender's Game" ) );

    SearcherWindow.Show(
        this, // this EditorWindow
        items, "Optional Title",
        item => { Debug.Log( item.name ); return /*close window?*/ true; },
        evt.mousePosition );
}
```


## Installing the Package

Open this file in your project:
```
Packages/manifest.json
```
Add this to the ```dependencies``` array (makes sure to change the version string to your current version):
```json
"com.unity.searcher": "4.0.0-preview"
```
For example, if this it he only package you depend on, you should have something like this (makes sure to change the version string to your current version):
```json
{
    "dependencies": {
        "com.unity.searcher": "4.0.0-preview"
    }
}
```

## Enabling the Samples and Tests

Right now, it seems Samples and Tests only show for local packages, meaning you cloned this repo *inside* your **Packages** folder. Given you've done that, open this file in your project:
```
Packages/manifest.json
```
Add a ```testables``` list with the package name so you get something like this (makes sure to change the version string to your current version):
```json
{
    "dependencies": {
        "com.unity.searcher": "4.0.0-preview"
    },
    "testables" : [ "com.unity.searcher" ]
}
```
You should see a new top-level menu called **Searcher** and you should see Searcher tests in **Test Runner**.
