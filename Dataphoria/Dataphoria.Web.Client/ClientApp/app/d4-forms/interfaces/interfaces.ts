import { EventEmitter } from '@angular/core';
import { IAction } from './action-interfaces';
import { IReadOnly, ISourceReferenceChild } from './data-interfaces';
import { INode, IVisual, HorizontalAlignment, VerticalAlignment } from './element-interfaces';


export interface IMenu extends IActionNode { };

export interface IExposed extends IActionNode { };

export interface ISearchColumn extends INode, IReadOnly, ISourceReferenceChild {
    Title: string,
    Hint: string,
    ColumnName: string,
    Width: number,
    TextAlignment: HorizontalAlignment
};

export interface IGridColumn extends INode, ISourceReferenceChild {
    Hint: string,
    Title: string,
    Width: number,
    Visible: boolean
};

export interface ITriggerColumn extends IGridColumn {
    Text: string,
    Action: IAction
};

export interface ISequenceColumn extends IGridColumn {
    Image: string,
    Script: string,
    ShouldEnlist: boolean
};

export interface IDataGridColumn extends IGridColumn {
    ColumnName: string
};

export interface ITextColumn extends IDataGridColumn {
    TextAlignment: HorizontalAlignment,
    MaxRows: number,
    VerticalAlignment: VerticalAlignment
    WordWrap: boolean,
    VerticalText: boolean
};

export interface IImageColumn extends IDataGridColumn {
    HorizontalAlignment: HorizontalAlignment,
    MaxRowHeight: number
};

export interface ICheckBoxColumn extends IDataGridColumn, IReadOnly { };

export interface ISourceLink {
    SourceLinkType: SourceLinkType,
    SourceLink: SourceLink 
};

export interface ITimer extends INode {
    AutoReset: boolean,
    Enabled: boolean,
    Interval: number,
    OnElapsed: IAction,
    Start(): void,
    Stop(): void
};

export interface IHost extends INode {
    Session: Session,
    Pipe: Pipe,
    NextRequest: Request,
    GetUniqueName(ANode: INode): void,
    Open(): void,
    Open(ADeferAfterActivate: boolean),
    AfterOpen(): void,
    Close(): void,
    Document: string,
    OnDocumentChanged: EventEmitter<Object>, // event, TODO: Figure out event to emit for DocumentChanged
    Load(ADocument: string, AInstance: Object),
    LoadNext(AInstance: Object): INode 
};

export interface IChildCollection extends Array<INode> { // System.Collections.IList
    // new INode this[int AIndex] { get; } // TODO: Figure out equivalent
    Disown(AItem: INode): void,
    DisownAt(AIndex: number): INode
};

export interface INode {
    Owner: INode,
    Parent: INode,
    Children: IChildCollection,
    IsValidChild(AChild: INode, AChildType?: string): boolean,
    IsValidOwner(AOwner: INode): boolean,
    IsValidOwner(AOwner: string): boolean,
    HostNode: IHost,
    FindParent(AType: string): INode,
    OnValidateName: EventEmitter<NameChangeHandler>, // event
    GetNode(AName: string, AExcluding: INode): INode,
    GetNode(AName: string): INode,
    FindNode(AName: string): INode,
    Transitional: boolean,
    Active: boolean,
    BroadcastEvent(AEvent: NodeEvent): void,
    HandleEvent(AEvent: NodeEvent): void,
    Name: string,
    UserData: Object
};

export interface IModule { };

// public delegate void NodeEventHandler(INode ANode, EventParams AParams);

export interface IBlockable extends INode {
    OnCompleted: NodeEventHandler // event
};

export interface IEnableable {
    GetEnabled(): boolean,
    Enabled: boolean
};

export interface IActionNode extends INode, IEnableable, IVisual {
    GetText(): string,
    Text: string,
    Action: IAction
};

export interface INodeReference {
    Node: INode
};

export class Request {
    private _document: string;
    Request(document: string) {       
        this._document = document
    };
    get Document(): string { return this._document; }
    set Document(document: string) { this._document = document; }
};

export abstract class NodeEvent {
    IsHandled: boolean;
    abstract Handle(node: INode): void;
};

// delegate
//export interface NameChangeHandler {
//    (ASender: Object, AOldName: string, ANewName: string): void
//}

export interface NameChangeHandler {
    ASender: Object,
    AOldName: string,
    ANewName: string
}