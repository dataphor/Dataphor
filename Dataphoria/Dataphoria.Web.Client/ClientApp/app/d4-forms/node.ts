import { EventEmitter } from '@angular/core';
import { INode, IChildCollection, IHost, INameChangeHandler, IUpdateHandler } from './interfaces';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { ArrayUtility } from './system';

export class Node implements INode {

    constructor() {
        this._children = new Array<Node>(this);
        //this._children = new ChildCollection(this);
    }

    //protected Dispose(disposing?: boolean): void {
    //    //try {

    //    //    // base.Dispose(disposing)
    //    //}
    //    //finally {
    //    //    if (this._children != null) {
    //    //        this._children.Dispose();
    //    //        this._children = null;
    //    //    }
    //    //}
    //};

    private _userData: Object;
    get UserData(): Object { return this._userData; }
    set UserData(value: Object) { this._userData = value; }

    protected _owner: Node;

    get Owner(): INode { return this._owner; }
    set Owner(value: INode) {
        if (this._owner !== value) {
            if (this._owner != null) {
                //this._owner.Children.Disown(this);
                this._owner.Remove(this);
            }
            if (value != null) {
                value.Children.push(this);
            }
        }
    }

    // NOTE: Virtual is the default in TS
    get Parent(): INode { return this._owner; }

    private _children: Array<Node>;
    //private _children: ChildCollection;

    get Children(): Array<Node> { return this._children; }
    //get Children(): ChildCollection { return this._children; }

    protected AddChild(child: INode): void { }
    protected RemoveChild(child: INode): void { }
    protected ChildrenChanged(): void { }

    IsValidChild(child?: INode, childType?: string): boolean {
        if (child == null || childType) {
            return false;
        } else {
            return this.IsValidChild(null, typeof child);
        };
    }

    IsValidOwner(owner: INode, ownerType: string): boolean {
        if (owner == null) {
            if (ownerType) {
                return true;
            } else {
                return false;
            }
        }
        return true;
    }

    protected InvalidChildError(child: INode): void {
        // throw new ClientException(ClientException.Codes.InvalidChild, child.Name, child.GetType().ToString(), GetType().ToString());
    }

    // type is typeof return value
    FindParent(type: string): INode {
        if (this.Parent != null) {
            // TODO: Figure out equivalent to/write our own 'type' lib
            // if (type.IsAssignableFrom(this.Parent.GetType()))
            //    return this.Parent;
            // else
            //    return this.Parent.FindParent(type);
            return this.Parent.FindParent(type);
        } else {
            return null;
        }
    }

    get HostNode(): IHost {
        let current: INode = this;
        while (current.Owner != null) {
            current = current.Owner;
        }
        return current as IHost;
    }

    GetNode(name: string, excluding?: INode): INode {
        if ((this.Name !== '') && (this.Name.toUpperCase() === name.toUpperCase()) && (excluding === this)) {
            if ((excluding && excluding === this) || !excluding) {
                return this;
            }
        } else {
            let result: INode;
            for (let child of this.Children) {
                result = child.GetNode(name, excluding);
                if (result != null) {
                    return result;
                };
            }
        }
        return null;
    }

    FindNode(name: string): INode {
        let result: INode = this.GetNode(name);
        if (result == null) {
            // throw new ClientException(ClientException.Codes.NodeNotFound, name);
        } else {
            return result;
        }
    }

    // event
    OnValidateName: BehaviorSubject<string>;

    private _name: string = '';

    get Name(): string { return this._name; }
    set Name(value: string) {
        if (this._name !== value) {
            if (value.indexOf(' ') !== -1) {
                // throw new ClientException(ClientException.Codes.InvalidNodeName, value);
            }
            if (this.OnValidateName != null) {
                //let _nameChangeHandler: INameChangeHandler;
                //_nameChangeHandler.ASender = this;
                //_nameChangeHandler.AOldName = this._name;
                //_nameChangeHandler.ANewName = value;
                this.OnValidateName.next(value);
            }
            this._name = value;
        }
    }

    toString():string {
        return this.Name;
    }

    //#region Activation/Deactivation

    private _transitional: boolean;

    get Transitional(): boolean {
        return this._transitional;
    }

    get Active(): boolean {
        return !this._transitional && (this.Owner != null) && this.Owner.Active;
    }

    protected CheckInactive(): void {
        if (this.Active) {
            // throw new ClientException(ClientException.Codes.NodeActive);
        }
    }

    protected CheckActive(): void {
        if (!this.Active) {
            // throw new ClientException(ClientException.Codes.NodeActive);
        }
    }

    protected Activate(): void {
        //for (let index: number = 0; index < this._children.length; index++) {
        //    try {
        //        // NOTE: Not sure why Activate needs to call ActivateAll on each node--
        //        // Deactivate just calls Deactivate on each... 
        //        this.Children[index].ActivateAll();
        //    } catch (exception) {
        //        for (let undoIndex: number = index - 1; undoIndex >= 0; undoIndex--) {
        //            this.Children[undoIndex].this.Deactivate();
        //        }
        //        throw new Error;
        //    }
        //}
    }

    protected AfterActivate(): void {
        // errors: ErrorList = null;
        for (let child of this.Children)
        {
            try {
                child.AfterActivate();
            } catch (exception) {
                // if (errors == null)
                //    errors = new ErrorList();
                // errors.Add(exception);
            }
        }
        // if (errors != null)
            // this.HostNode.Session.ReportErrors(this.HostNode, errors);
    }

    protected Deactivate(): void {
        //if (this.Children != null) {
        //    // ErrorList errors = null;
        //    for (let child of this.Children.Node.Children)
        //    {
        //        try {
        //            child.Deactivate();
        //        } catch (exception) {
        //            // if (errors == null)
        //            //    errors = new ErrorList();
        //            // errors.Add(exception);
        //        }
        //    }
        //    // if (errors != null)
        //    //    HostNode.Session.ReportErrors(HostNode, errors);
        //}
    }

    protected BeforeDeactivate(): void {
        //if (this.Children != null) {
        //    // ErrorList errors = null;
        //    for (let child of this.Children.Node.Children)
        //    {
        //        try {
        //            child.BeforeDeactivate();
        //        } catch (exception) {
        //            // if (errors == null)
        //            //    errors = new ErrorList();
        //            // errors.Add(exception);
        //        }
        //    }
        //    // if (errors != null)
        //    //    HostNode.Session.ReportErrors(HostNode, errors);
        //}
    }

    DeactivateAll(): void {
        //this._transitional = true;
        //try {
        //    this.Deactivate();
        //} finally {
        //    this._transitional = false;
        //}
    }

    ActivateAll(): void {
        //this._transitional = true;
        //try {
        //    this.Activate();
        //} finally {
        //    this._transitional = false;
        //}
    }

    //#endregion

    //#region Broadcast/Handle events

    //BroadcastEvent(eventValue: INodeEvent): void {
    //    if (!eventValue.IsHandled) {
    //        this.HandleEvent(eventValue);
    //        if (!eventValue.IsHandled) {
    //            eventValue.Handle(this);
    //            if (!eventValue.IsHandled) {
    //                for (let child of this.Children) {
    //                    child.BroadcastEvent(eventValue);
    //                    if (eventValue.IsHandled) {
    //                        break;
    //                    }
    //                }
    //            }
    //        }
    //    }
    //}

    //HandleEvent(eventValue: INodeEvent): void { }

    //#endregion

    //#region IComponent

    //private _site: ISite;

    //get Site(): ISite {
    //    return this._site;
    //}
    //set Site(value: ISite) {
    //    this._site = value;
    //}

    //#endregion

    //#region Ownership
    

    Remove(tempValue: INode): void {
        this._children = ArrayUtility.Remove(tempValue, this._children);
    }

    protected _disowning: boolean;

    Disown(item: INode): INode {
        //this._disowning = true;
        //try {
        //    this.Remove(item);
        //    return item;
        //}
        return item; 
    }

    DisownAt(index: number): INode {
        //this._disowning = true;
        //try {
        //    return ArrayUtility.RemoveAt(index, this._children);
        //} finally {
        //    this._disowning = false;
        //}
        return ArrayUtility[index];
    }

    //#endregion

};


//export class ChildCollection implements IChildCollection {

//    //constructor(...nodes: Array<INode>) {
//    //    super();
//    //    this.push(...nodes);
//    //}

//    constructor(node: INode) {
//        this._node = node;
//    }

//    protected _node: Node;

//    get Node(): INode {
//        return this._node;
//    }
//    set Node(value: INode) {
//        this._node = value;
//    }

//    protected Adding(value: Node, index: number): void {
//        value.Owner = null;
//        if (value.Active) {
//            // throw new ClientException(ClientException.Codes.CannotAddActiveChild);
//        }

//        // super.Adding(value, index);

//        this._node.AddChild(value);
//        try {
//            value._owner = this._node;
//            try {
//                if (this._node.Active) {
//                    let handler: INode = this._node.FindParent(typeof this._node);
//                }
//            }
//        }
//    }

//};