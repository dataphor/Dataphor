import { INode, IAction } from './element-interfaces';

export interface ISource extends INode {
    ActiveChanged: DataLinkHandler; // event; DAE
    StateChanged: DataLinkHandler; // event; DAE
    DataChanged: DataLinkHandler; // event; DAE
    RowChanging: DataLinkFieldHandler; // event; DAE
    RowChanged: DataLinkFieldHandler; // event; DAE
    Default: DataLinkFieldHandler; // event; DAE
    DataSource: DataSource; // DAE
    DataView: DataView; // DAE; needs alias (DataView taken by es2015)
    OnUpdateView: EventHandler; // event; System
    Process: IServerProcess; // DAE
    TableVar: Schema.TableVar; // DAE 
    Order: Schema.Order; // DAE
    OrderString: string;
    // this[AColumnName: string]: DataField; // Not sure of equivalent here
    // this[AColumnIndex: number]: DataField; // Not sure of equivalent here
    State: DataSetState; // DAE
    BOF: boolean;
    EOF: boolean;
    IsEmpty: boolean;
    First(): void;
    Prior(): void;
    Next(): void;
    Last(): void;
    GetKey(): DAE.Runtime.Data.IRow; // DAE
    FindKey(AKey: DAE.Runtime.Data.IRow): boolean; // DAE
    FindNearest(AKey: DAE.Runtime.Data.IRow): boolean; // DAE
    Refresh(): void;
    IsModified: boolean;
    Insert(): void;
    Edit(): void;
    Post(): void;
    Cancel(): void;
    Delete(): void;
    Expression: string;
    Enabled: boolean;
    Filter: string;
    Params: DataParams; // DAE
    BeginScript: string;
    EndScript: string;
    Surrogate: ISource;
    MasterKeyNames: string;
    DetailKeyNames: string;
    Master: ISource;
    UseBrowse: boolean;
    UseApplicationTransactions: boolean;
    IsReadOnly: boolean;
    IsWriteOnly: boolean;
    RefreshAfterPost: boolean;
    ShouldEnlist: EnlistMode; // DAE
    OpenState: DataSetState; // DAE
    OnChange: IAction;
    OnActiveChange: IAction;
    OnStateChange: IAction;
    OnRowChanging: IAction;
    OnRowChange: IAction;
    OnDefault: IAction;
    OnValidate: IAction;
    BeforeOpen: IAction;
    AfterOpen: IAction;
    BeforeClose: IAction;
    AfterClose: IAction;
    BeforeInsert: IAction;
    AfterInsert: IAction;
    BeforeEdit: IAction;
    AfterEdit: IAction;
    BeforeDelete: IAction;
    AfterDelete: IAction;
    BeforePost: IAction;
    AfterPost: IAction;
    BeforeCancel: IAction;
    AfterCancel: IAction;
    WriteWhereClause: boolean;
    InsertStatement: string;
    UpdateStatement: string;
    DeleteStatement: string;
    CursorType: DAE.CursorType; // DAE
    IsolationLevel: DAE.IsolationLevel; // DAE
    RequestedIsolation: DAE.CursorIsolation; // DAE
};

export interface IDataDefault {
    Enabled: boolean;
    TargetColumns: string;
};

export interface IDataValueDefault extends IDataDefault {
    SourceValues: string;
};

export interface IDataSourceDefault extends IDataDefault {
    Source: ISource;
    SourceColumns: string;
};

export interface ISourceChild { };

export interface ISourceReference {
    Source: ISource;
};

export interface ISourceReferenceChild { };

export interface IReadOnly {
    ReadOnly: boolean;
};

export class ViewActionEvent extends NodeEvent {

    constructor(action: SourceActions): void {
        super();
        this.Action = action;
    }

    Action: SourceActions;

};

