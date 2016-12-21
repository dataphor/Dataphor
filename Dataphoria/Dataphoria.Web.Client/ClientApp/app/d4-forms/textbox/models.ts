import { HelpKeywordBehavior, TitleAlignment, VerticalAlignment } from '../models';
import { ISource } from '../interfaces';

export interface textboxbase {
    TextAlignment: number,
    Width: number,
    MaxWidth: number,
    ColumnName: string,
    Title: string,
    Hint: string,
    MarginLeft: number,
    MarginRight: number,
    MarginTop: number,
    MarginBottom: number,
    HelpKeywordBehavior: HelpKeywordBehavior,
    Name: string,
    UserData: Object,
    ReadOnly: boolean,
    Source: ISource,
    TitleAlignment: TitleAlignment,
    VerticalAlignment: VerticalAlignment,
    Visible: boolean,
    NilIfBlank: boolean
};