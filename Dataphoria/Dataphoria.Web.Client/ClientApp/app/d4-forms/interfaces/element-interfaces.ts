import { IAction, IDocumentReference, FormMode, CloseBehavior } from './action-interfaces';
import { ISource, ISourceReference, IReadOnly } from './data-interfaces';
import { IEnableable, ISourceLink } from './interfaces';


export interface IHorizontalAlignedElement {
    HorizontalAlignment: HorizontalAlignment
};

export interface IVerticalAlignedElement {
    VerticalAlignment: VerticalAlignment
};

export interface IRow extends IElement, IHorizontalAlignedElement { };

export interface IColumn extends IElement, IHorizontalAlignedElement { };

export interface IGroup extends IElement {
    Title: string
};

export interface IDateTimeBox extends IAlignedElement {
    AutoUpdate: boolean,
    AutoUpdateInterval: number
};

export interface ITrigger extends IElement, IEnableable, IVerticalAlignedElement {
    GetText(): string;
    Text: string,
    Action: IAction,
    ImageWidth: number,
    ImageHeight: number
};

export interface IInterface extends IElement {
    CheckMainSource(): void,
    GetText(): string,
    PerformDefaultAction(): string,
    PostChanges(): void,
    PostChangesIfModified(): void,
    CancelChanges(): void,
    Text: string,
    MainSource: ISource,
    OnDefault: IAction,
    OnShown: IAction,
    OnPost: IAction,
    OnCancel: IAction,
    OnActivate: IAction,
    OnAfterActivate: IAction,
    OnBeforeDeactivate: IAction,
    UserState: IndexedDictionary<string, object>
};

export interface IImage extends ITitledElement {
    Width: number,
    ImageWidth: number,
    ImageHeight: number,
    StretchStyle: StretchStyles,
    Center: boolean,
    ImageSource: IImageSource
};

export interface IImageSource {
    Stream: System.IO.Stream,
    LoadImage(): void,
    Loading: boolean
};

export interface IText extends IAlignedElement {
    Height: number
};

export interface ITextBoxBase extends IAlignedElement {
    NilIfBlank: boolean
};

export interface ITextBox extends ITextBoxBase {
    Height: number,
    IsPassword: boolean,
    WordWrap: boolean,
    AcceptsTabs: boolean,
    AcceptsReturn: boolean,
    MaxLength: number
};

export interface INumericTextBox extends ITextBoxBase {
    SmallIncrement: number,
    LargeIncrement: number
};

export interface ITextEditor extends ITitledElement {
    NilIfBlank: boolean,
    Height: number,
    DocumentType: string
};

export interface ISearch extends IDataElement {
    TitleAlignment: TitleAlignment
};

export interface IGrid extends IDataElement {
    RowCount: number,
    ColumnCount: number,
    OnDoubleClick: IAction,
    UseNaturalMaxWidth: boolean
};

export interface ILookupElement extends IElement, ILookup, IReadOnly {
    ResetAutoLookup(): void,
    AutoLookup: boolean,
    AutoLookupWhenNotNil: boolean
};

export interface IQuickLookup extends ILookupElement, IVerticalAlignedElement {
    ColumnName: string,
    LookUpColumnName: string
};

export interface IFullLookup extends ILookupElement {
    GetTitle(): string,
    Title: string,
    ColumnNames: string,
    LookupColumnNames: string
};

export interface ICheckBox extends IColumnElement {
    Width: number,
    AutoUpdate: boolean,
    AutoUpdateInterval: number,
    TrueFirst: boolean
};

export interface IChoice extends IColumnElement {
    Items: string,
    AutoUpdate: boolean,
    AutoUpdateInterval: number,
    Columns: number
};

export interface ITree extends IDataElement {
    RootExpression: string,
    ChildExpression: string,
    ParentExpression: string,
    Width: number,
    Height: number
};

export interface IStaticImage extends IElement, IVerticalAlignedElement, IHorizontalAlignedElement {
    ImageWidth: number,
    ImageHeight: number,
    StretchStyle: StretchStyles,
    Image: string
};

export interface IStaticText extends IElement {
    Text: string,
    Width: number
};

export interface IHtmlBox extends IElement {
    PixelWidth: number,
    PixelHeight: number,
    URL: string
};

export interface INoteBook extends IElement {
    ActivePage: IBaseNotebookPage,
    OnActivePageChange: IAction
};

export interface IBaseNotebookPage extends IElement {
    Title: string
};

export interface INotebookPage extends IBaseNotebookPage { };

export interface INotebookFramePage extends IBaseNotebookPage, ISourceLink {
    FrameInterfaceNode: IFrameInterface,
    Document: string,
    MenuText: string,
    LoadAsSelected: boolean
};

export interface IFile extends ITitledElement {
    ExtensionColumnName: string,
    MaximumContentLength: number
};

export interface IVisual {
    GetVisible(): boolean,
    Visible: boolean
};

export interface IElement extends INode, IVisual {
    GetHint(): string,
    GetTabStop(): boolean,
    Hint: string,
    TabStop: boolean,
    MarginLeft: number,
    MarginRight: number,
    MarginTop: number,
    MarginBottom: number,
    HelpKeyword: string
    HelpKeywordBehavior: HelpKeywordBehavior,
    HelpString: string
    Style: string
};

export interface IFormInterface extends IInterface {
    Show(): void,
    Show(AFormMode: FormMode): void,
    Show(AParentForm: IFormInterface, AOnAcceptForm: FormInterfaceHandler, AOnRejectForm: FormInterfaceHandler, AFormMode: FormMode),
    Mode: FormMode,
    EmbedErrors(AErrorList: ErrorList): void, // TODO: Create Error Handling of some sort
    Close(ABehavior: CloseBehavior): boolean,
    IsAcceptReject: boolean,
    AcceptEnabled: boolean,
    Enable(): void,
    Disable(AChildForm: IFormInterface): void,
    GetEnabled(): boolean
    OnClosed: EventHandler,
    OnBeforeAccept: IAction,
    ForceAcceptReject: boolean,
    TopMost: boolean
};

export interface IFrame extends IElement, ISourceLink {
    FrameInterfaceNode: IFrameInterface,
    PostBeforeClosingEmbedded: boolean,
    BeforeCloseEmbedded: IAction,
    Document: string,
    Filter: string,
    MenuText: string
};

export interface IFrameInterface extends IInterface {
    Frame: IFrame
};

export interface IDataElement extends IElement, ISourceReference, IReadOnly { };

export interface IColumnElement extends IDataElement {
    ColumnName: string,
    Title: string
};

export interface ITitledElement extends IColumnElement, IVerticalAlignedElement {
    TitleAlignment: TitleAlignment;
};

export interface IAlignedElement extends ITitledElement {
    TextAlignment: HorizontalAlignment,
    Width: number,
    MaxWidth: number
};

export interface ILookup extends INode, ISourceReference, IDocumentReference {
    LookupFormInitialize(AForm: IFormInterface): void,
    GetColumnNames(): string,
    GetLookupColumnNames(): string,
    MasterKeyNames: string,
    DetailKeyNames: string
};

export interface ILayoutDisableable {
    DisableLayout(): void,
    EnableLayout(): void
};

export interface IUpdateHandler {
    BeginUpdate(): void,
    EndUpdate(): void,
    EndUpdate(APerformLayout: boolean)
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
    Right = 1,
    Center= 2
};

export enum TextAlignment {
    Left = 0,
    Right,
    Center
};

export enum HelpKeywordBehavior {
    AssociateIndex = -2147483643,	//System.Windows.Forms.HelpNavigator.AssociateIndex
    Find = -2147483644,				//System.Windows.Forms.HelpNavigator.Find
    Index = -2147483645,			//System.Windows.Forms.HelpNavigator.Index
    KeywordIndex = -2147483642,		//System.Windows.Forms.HelpNavigator.KeywordIndex
    TableOfContents = -2147483646,	//System.Windows.Forms.HelpNavigator.TableOfContents
    Topic = -2147483647,			//System.Windows.Forms.HelpNavigator.Topic
    TopicId = -2147483641			//System.Windows.Forms.HelpNavigator.TopicId
};

// Interface
export interface FormInterfaceHandler {
    (AForm: IFormInterface): void
};

export enum StretchStyles {
    StretchRatio = 0,
    StretchFill,
    NoStretch
};