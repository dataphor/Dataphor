import { Input, EventEmitter } from '@angular/core';
import { INode, ISource, IAction, IChildCollection, IHost, NameChangeHandler } from './interfaces';


export class Node implements INode {

    constructor() {
        //super();
        this._children = new ChildCollection(this);
    }

    protected Dispose(disposing: boolean): void {
        try {
            //base.Dispose(disposing)
        }
        finally {
            if (this._children != null) {
                this._children.Dispose();
                this._children = null;
            }
        }
    };

    private _userData: Object;
    get(): Object { return this._userData; }
    set(value: Object): void { this._userData = value; }

    protected _owner: Node;

    get Owner(): INode { return this._owner; }
    set Owner(value: INode) {
        if (this._owner != value) {
            if (this._owner != null)
                this._owner.Children.Disown(this);
            if (value != null)
                value.Children.Add(this);
        }
    }

    // NOTE: Virtual is the default in TS
    get Parent(): INode { return this._owner; }

    private _children: ChildCollection;

    get Children(): IChildCollection { return this._children; }

    protected AddChild(child: INode): void { }
    protected RemoveChild(child: INode): void { }
    protected ChildrenChanged(): void { }

    IsValidChild(child: INode): boolean {
        if (child == null) {
            return false;
        } else return this.IsValidChild(typeof child);
    }

    // child is typeof return value
    IsValidChild(child: string): boolean {
        return false;
    }

    IsValidOwner(ownerType: string) {
        return true;
    }

    protected InvalidChildError(child: INode): void {
        // throw new ClientException(ClientException.Codes.InvalidChild, child.Name, child.GetType().ToString(), GetType().ToString());
    }

    // type is typeof return value
    FindParent(type: string): INode {
        if (this.Parent != null) {
            // TODO: Figure out equivalent to/write our own 'type' lib
            //if (type.IsAssignableFrom(this.Parent.GetType()))
            //    return this.Parent;
            //else
            //    return this.Parent.FindParent(type);
            return this.Parent.FindParent(type)
        } else {
            return null;
        }
    }

    get HostNode(): IHost {
        let current: INode = this;
        while (current.Owner != null) {
            current = current.Owner
        }
        return current as IHost;
    }

    GetNode(name: string, excluding: INode): INode {
        if ((this.Name != '') && (this.Name.toUpperCase() == name.toUpperCase()) && (excluding == this)) {
            return this;
        } else {
            let result: INode;
            for (let child: INode of this.Children) {
                result = child.GetNode(name, excluding);
                if (result != null) return result;
            }
        }
        return null;
    }

    GetNode(name: string): INode {
        if ((this.Name != '') && (this.Name.toUpperCase() == name.toUpperCase())) {
            return this;
        } else {
            let result: INode;
            for (let child: INode of this.Children) {
                result = child.GetNode(name, excluding);
                if (result != null) return result;
            }
        }
        return null;
    }

    FindNode(name: string): INode {
        let result: INode = this.GetNode(name);
        if (result == null) {
            //throw new ClientException(ClientException.Codes.NodeNotFound, name);
        } else {
            return result;
        }
    }

    //event
    OnValidateName: EventEmitter<NameChangeHandler>;

    private _name: string = '';

    get Name() { return this._name; }
    set Name(value: string) {
        if (this._name != value) {
            if (value.indexOf(' ') != -1) {
                //throw new ClientException(ClientException.Codes.InvalidNodeName, value);
            }
            if (this.OnValidateName != null) {
                let _nameChangeHandler: NameChangeHandler;
                _nameChangeHandler.ASender = this;
                _nameChangeHandler.AOldName = this._name;
                _nameChangeHandler.ANewName = value;
                this.OnValidateName.emit(_nameChangeHandler);
            }
            this._name = value;
        }
    }

    toString() {
        return this.Name;
    }

    //#region Activation/Deactivation

    get Transitional(): boolean {
        return this._transitional;
    }

    get Active(): boolean {
        return !this._transitional && (this.Owner != null) && this.Owner.Active;
    }

    protected CheckInactive(): void {
        if (this.Active) {
            //throw new ClientException(ClientException.Codes.NodeActive);
        }
    }

    protected CheckActive(): void {
        if (!this.Active) {
            //throw new ClientException(ClientException.Codes.NodeActive);
        }
    }

    protected Activate(): void {
        for (let index = 0; index < this._children.length; index++) {
            try {
                // NOTE: Not sure why Activate needs to call ActivateAll on each node--
                // Deactivate just calls Deactivate on each... 
                this.Children[index].ActivateAll();
            } catch (exception) {
                for (let undoIndex = index - 1; undoIndex >= 0; undoIndex--) {
                    this.Children[undoIndex].this.Deactivate();
                }
                throw new Error;
            }
        }
    }

    protected AfterActivate(): void {
        //errors: ErrorList = null;
        for (let child of this.Children)
        {
            try {
                child.AfterActivate();
            }
            catch (exception) {
                //if (errors == null)
                //    errors = new ErrorList();
                //errors.Add(exception);
            }
        }
        //if (errors != null)
            //this.HostNode.Session.ReportErrors(this.HostNode, errors);
    }

    protected Deactivate(): void {
        if (this.Children != null) {
            //ErrorList errors = null;
            for (let child of this.Children)
            {
                try {
                    child.Deactivate();
                }
                catch (exception) {
                    //if (errors == null)
                    //    errors = new ErrorList();
                    //errors.Add(exception);
                }
            }
            //if (errors != null)
            //    HostNode.Session.ReportErrors(HostNode, errors);
        }
    }

    protected BeforeDeactivate(): void {
        if (this.Children != null) {
            //ErrorList errors = null;
            for (let child of this.Children)
            {
                try {
                    child.BeforeDeactivate();
                }
                catch (exception) {
                    //if (errors == null)
                    //    errors = new ErrorList();
                    //errors.Add(exception);
                }
            }
            //if (errors != null)
            //    HostNode.Session.ReportErrors(HostNode, errors);
        }
    }

    DeactivateAll(): void {
        this._transitional = true;
        try {
            this.Deactivate();
        } finally {
            this._transitional = false;
        }
    }

    ActivateAll(): void {
        this._transitional = true;
        try {
            this.Activate();
        } finally {
            this._transitional = false;
        }

    }

    //#endregion


};

export class ChildCollection implements IChildCollection {

    constructor(node: Node) {
        //super(true);
        this._node = node;
    }

    protected _node: Node;

    protected Adding(value: Node, index: number): void {
        value.Owner = null;
        if (value.Active) {
            //throw new ClientException(ClientException.Codes.CannotAddActiveChild);
        }
    }

};