import { HelpKeywordBehavior, TitleAlignment, VerticalAlignment } from './models';

export interface IElement {
    Name: string,
    UserData: Object,
    Visible: boolean,
    Hint: string,
    MarginLeft: number,
    MarginRight: number,
    MarginTop: number,
    MarginBottom: number,
    HelpKeyword: string
    HelpKeywordBehavior: HelpKeywordBehavior,
    HelpString: string
}

export interface ISource {
    Name: string,
    UserData: Object,
    WriteWhereClause: boolean,
    DataSource: string, // simplified
    DataView: string, // simplified
    Process: string, // simplified
    TableVar: string, // simplified
    Order: string, // simplified
    OrderString: string,
    Item: string, // simplified
    State: string, // simplified
    BOF: boolean,
    EOF: boolean,
    IsEmpty: boolean,
    IsModified: boolean,
    Expression: string,
    Filter: string,
    BeginScript: string,
    EndScript: string,
    Surrogate: ISource,
    MasterKeyNames: string,
    DetailKeyNames: string,
    Master: ISource,
    UseBrowse: boolean,
    UseApplicationTransactions: boolean,
    ShouldEnlist: string, // simplified
    OpenState: string, // simplified
    OnChange: IAction,
    OnActiveChange: IAction,
    OnStateChange: IAction,
    OnRowChanging: IAction,
    OnRowChange: IAction,
    OnDefault: IAction,
    OnValidate: IAction,
    BeforeOpen: IAction,
    AfterOpen: IAction,
    BeforeClose: IAction,
    AfterClose: IAction,
    BeforeInsert: IAction,
    AfterInsert: IAction,
    BeforeEdit: IAction,
    AfterEdit: IAction,
    BeforeDelete: IAction,
    AfterDelete: IAction,
    BeforePost: IAction,
    AfterPost: IAction,
    BeforeCancel: IAction,
    AfterCancel: IAction,
    InsertStatement: string,
    UpdateStatement: string,
    DeleteStatement: string,
    CursorType: string, // simplified
    IsolationLevel: string, // simplified
    RequestedIsolation: string // simplified
}

export interface IAction {
    Enabled: boolean,
    Name: string,
    UserData: Object,
    // TODO: Create new Image Type
    LoadedImage: string,
    Text: string,
    Hint: string,
    Image: string,
    BeforeExecute: IAction,
    AfterExecute: IAction,
    Visible: boolean
}

export interface ITitledElement {
    ColumnName: string,
    Hint: string,
    MarginLeft: number,
    MarginRight: number,
    MarginTop: number
    MarginBottom: number,
    HelpKeywordBehavior: HelpKeywordBehavior,
    Name: string,
    UserData: Object,
    ReadOnly: boolean,
    Source: ISource,
    VerticalAlignment: VerticalAlignment,
    Visible: boolean,
    TitleAlignment: TitleAlignment;
}