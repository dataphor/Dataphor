import { Input } from '@angular/core';
import { INode, ISource, IAction } from './interfaces';


export class Source implements ISource, INode {

    Name: string;
    UserData: Object;
    WriteWhereClause: boolean;
    DataSource: string; // simplified
    DataView: string; // simplified
    Process: string; // simplified
    TableVar: string; // simplified
    Order: string; // simplified
    OrderString: string;
    Item: string; // simplified
    State: string; // simplified
    BOF: boolean;
    EOF: boolean;
    IsEmpty: boolean;
    IsModified: boolean;
    Expression: string;
    Filter: string;
    BeginScript: string;
    EndScript: string;
    Surrogate: ISource;
    MasterKeyNames: string;
    DetailKeyNames: string;
    Master: ISource;
    UseBrowse: boolean;
    UseApplicationTransactions: boolean;
    ShouldEnlist: string; // simplified
    OpenState: string; // simplified
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
    InsertStatement: string;
    UpdateStatement: string;
    DeleteStatement: string;
    CursorType: string; // simplified
    IsolationLevel: string; // simplified
    RequestedIsolation: string // simplified

    constructor(_source: ISource) {
        this.Name = _source.Name ? _source.Name : null;
        this.UserData = _source.UserData ? _source.UserData : Object;
        this.WriteWhereClause = _source.WriteWhereClause ? _source.WriteWhereClause : false;
        this.DataSource = _source.DataSource ? _source.DataSource : '';
        this.DataView = _source.DataView ? _source.DataView : '';
        this.Process = _source.Process ? _source.Process : '';
        this.TableVar = _source.TableVar ? _source.TableVar : '';
        this.Order: string; // simplified
        this.OrderString: string;
        this.Item: string; // simplified
        this.State: string; // simplified
        this.BOF: boolean;
        this.EOF: boolean;
        this.IsEmpty: boolean;
        this.IsModified: boolean;
        this.Expression: string;
        this.Filter: string;
        this.BeginScript: string;
        this.EndScript: string;
        this.Surrogate: ISource;
        this.MasterKeyNames: string;
        this.DetailKeyNames: string;
        this.Master: ISource;
        this.UseBrowse: boolean;
        this.UseApplicationTransactions: boolean;
        this.ShouldEnlist: string; // simplified
        this.OpenState: string; // simplified
        this.OnChange: IAction;
        this.OnActiveChange: IAction;
        this.OnStateChange: IAction;
        this.OnRowChanging: IAction;
        this.OnRowChange: IAction;
        this.OnDefault: IAction;
        this.OnValidate: IAction;
        this.BeforeOpen: IAction;
        this.AfterOpen: IAction;
        this.BeforeClose: IAction;
        this.AfterClose: IAction;
        this.BeforeInsert: IAction;
        this.AfterInsert: IAction;
        this.BeforeEdit: IAction;
        this.AfterEdit: IAction;
        this.BeforeDelete: IAction;
        this.AfterDelete: IAction;
        this.BeforePost: IAction;
        this.AfterPost: IAction;
        this.BeforeCancel: IAction;
        this.AfterCancel: IAction;
        this.InsertStatement: string;
        this.UpdateStatement: string;
        this.DeleteStatement: string;
        this.CursorType: string; // simplified
        this.IsolationLevel: string; // simplified
        this.RequestedIsolation: string // simplified
    }
}
