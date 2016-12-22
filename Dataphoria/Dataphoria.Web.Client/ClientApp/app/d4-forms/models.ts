import { Input } from '@angular/core';

export enum HelpKeywordBehavior {
    AssociateIndex,
    Find,
    Index,
    KeywordIndex = 0,
    TableOfContents,
    Topic
};

export enum TitleAlignment {
    Top = 0,
    Left,
    None
};

export enum VerticalAlignment {
    Top = 0,
    Middle,
    Bottom
};

export enum HorizontalAlignment {
    Left = 0,
    Right,
    Center
};

export enum TextAlignment {
    Left = 0,
    Right,
    Center
};

// Overwrites default boolean class, because it uses true and false (no caps)
export enum Boolean {
    True,
    False = 0
};
