import { ILookup, HelpKeywordBehavior } from './element-interfaces';
import { ISource } from './data-interfaces';
import { INode, ISourceLink, IEnableable, IBlockable } from './interfaces';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { NotifyIcon } from '../action';

export interface IAction extends INode, IEnableable {
    OnTextChanged$: BehaviorSubject<string>, // event
    OnEnabledChanged$: BehaviorSubject<boolean>, // event
    OnHintChanged$: BehaviorSubject<string>, // event
    OnImageChanged$: BehaviorSubject<string>, // event
    OnVisibleChanged$: BehaviorSubject<boolean>, // event
    GetText(): string,
    GetDescription(): string,
    Execute(): void,
    Execute(ASender: INode, ATarget: INode): void,
    Text: string,
    Hint: string,
    Image: string,
    BeforeExecute: IAction,
    AfterExecute: IAction,
    Visible: boolean
};

export interface IConditionalAction extends IAction {
    Condition: string
};

export interface IBlockAction extends IAction { };

export interface ICallAction extends IAction {
    Action: IAction
};

export interface IHelpAction extends IAction {
    HelpKeyword: string,
    HelpKeywordBehavior: HelpKeywordBehavior,
    HelpString: string
};

export interface INotifyAction extends IAction {
    TipTitle: string,
    TipText: string,
    TipAction: NotifyIcon // Done in Action.cs -- Have to look into our approach here
};

export interface ISetPropertyAction extends IAction {
    Node: INode,
    MemberName: string,
    Value: string
};

export interface IShowLinkAction extends IAction {
    URL: string
};

export interface ISourceAction extends IDataAction {
    Action: SourceActions
};

export interface IFindAction extends IDataAction {
    Mode: FindActionMode,
    ColumnName: string,
    Value: string
};

export interface IDataScriptAction extends IAction {
    String: string,
    EnlistWisth: ISource
};

export interface IBaseArgument extends INode { };

export interface IUserStateArgument extends IBaseArgument {
    KeyName: string,
    DefaultValue: string,
    ParamName: string,
    Modifier: DAE.Language.Modifier // DAE
};

export interface IDataArgument extends IBaseArgument {
    Source: ISource,
    Columns: string,
    Modifier: DAE.Language.Modifier // DAE
};

export interface IFormAction extends IAction {
    Behavior: CloseBehavior
};

export interface ISetNextRequestAction extends IAction, IDocumentReference { };

export interface IClearNextRequestAction extends IAction { };

export interface IExecuteModuleAction extends IAction {
    ModuleExpression: string,
    ActionName: string
};

export interface IShowFormAction extends IAction, IDocumentReference, ISourceLink, IBlockable {
    SourceLinkRefresh: boolean,
    Mode: FormMode,
    UseOpenState: boolean,
    ManageWriteOnly: boolean,
    ManageRefreshAfterPost: boolean,
    ConfirmDelete: boolean,
    AutoAcceptAfterInsertOnQuery: boolean,
    Filter: string,
    TopMost: boolean,
    OnFormClose: IAction,
    OnFormAccepted: IAction,
    OnFormRejected: IAction,
    BeforeFormActivated: IAction,
    AfterFormActivated: IAction
};

export interface IEditFilterAction extends IDataAction { };

export interface IScriptAction extends IAction {
    Language: ScriptLanguages,
    Script: string,
    ScriptDocument: string,
    References: string,
    CompilesWithDebug: boolean
};

export interface ISetArgumentsFromParams extends IAction { };

export interface ISetArgumentsFromMainSource extends IAction { };

export interface ICopyAction extends IAction {
    ClipboardFormatName: string
};

export interface IPasteAction extends IAction {
    ClipboardFormatName: string
};

export interface IDataAction extends IAction {
    Source: ISource
};

export interface IDocumentReference {
    Document: string
};

export interface ILookupAction extends IAction, ILookup {
    AutoPost: boolean, 
    OnLookupRejected: IAction,
    OnLookupAccepted: IAction,
    OnLookupClosed: IAction,
    BeforeLookupActivated: IAction
};

export enum SourceActions {
    First,
    Prior,
    Next,
    Last,
    Refresh,
    Insert,
    Append,
    Edit,
    Delete,
    Post,
    PostDetails,
    Cancel,
    RequestSave,
    Validate,
    Close,
    Open,
    PostIfModified
};

export enum FindActionMode {
    Nearest,
    Exact,
    ExactOnly
};

export enum FormMode {
    None,
    Insert,
    Edit,
    Delete,
    Query
};

export enum CloseBehavior {
    RejectOrClose,
    AcceptOrClose
};

export enum ScriptLanguages {
    CSharp,
    VisualBasic
};