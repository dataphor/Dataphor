import { Node } from './node';
import { INode, IAction, ILayoutDisableable, IBlockable } from './interfaces';
import { KeyedCollection } from './system';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';

export enum NotifyIcon {
    None,
    Info,
    Warning,
    Error
}

//export class EventParams extends KeyedCollection<Object> {
//    constructor(parameters?: Array<Object>) {
//        super();
//        if (parameters) {
//            for (let index: number = 0; index < parameters.length; index++) {
//                if ((index % 2) != 0) {
//                    super.Add(parameters[index - 1].toString(), parameters[index]);
//                }
//            }
//        }
//    }
//}

export class Action extends Node implements IAction {

    Dispose(disposing: boolean): void {

    }

    Execute(sender?: INode, target?: INode): void {
        if (sender && target) {
            if (this.GetEnabled()) {
                // TODO: Figure out equivalent
                //let layoutDisableable: ILayoutDisableable = super.FindParent(typeof ILayoutDisableable) as ILayoutDisableable;
                //if (layoutDisableable !== null) {
                //    layoutDisableable.DisableLayout();
                //}
                try {
                    if (this.DoBeforeExecute(sender, target)) {
                        this.FinishExecute(sender, target);
                    }
                } finally {
                    //if (layoutDisableable !== null) {
                    //    ILayoutDisableable.EnableLayout();
                    //}
                }
            }
        }
    }

    InternalExecute(sender: INode, target: INode) { }

    private _beforeExecute: IAction;

    get BeforeExecute(): IAction {
        return this._beforeExecute;
    }
    set BeforeExecute(value: IAction) {
        if (this._beforeExecute !== value) {
            this._beforeExecute = value;
        }
    }

    //private BeforeExecuteDisposed(sender: Object, args: EventArgs) {
    //    BeforeExecute = null;
    //}

    private DoBeforeExecute(sender: INode, target: INode): boolean {
        if (this._beforeExecute !== null) {
            let blockable: IBlockable = this._beforeExecute as IBlockable;
            if (blockable !== null) {
                blockable.OnCompleted$.subscribe({
                    complete: () => {
                        this.BeforeExecuteCompleted(sender, target);
                    }
                });

                this._beforeExecute.Execute(this, target);

                return blockable === null;

            }

            return true;

        }
    }

    private BeforeExecuteCompleted(sender: INode, target: INode): void {
        let blockable: IBlockable = this._beforeExecute as IBlockable;
        if (blockable !== null) {
            blockable.OnCompleted$.unsubscribe();
        }
        this.FinishExecute(sender, target);
    }

    private FinishExecute(sender: INode, target: INode): void {
        this.InternalExecute(sender, target);
        this.DoAfterExecute(target);
    }

    private _afterExecute: IAction;

    get AfterExecute(): IAction {
        return this._afterExecute;
    }
    set AfterExecute(value: IAction) {
        if (this._afterExecute !== value) {
            if (this._afterExecute !== null) {

                // On AfterExecuteDisposed being called, 

                this._afterExecute.Disposed
            }
        }
    }



}

export class BlockableAction extends Action implements IBlockable {

    private _onCompleted = new BehaviorSubject<INode>(null);

    OnCompleted$ = this._onCompleted.asObservable();

    protected DoCompleted(node: INode): void {
        this._onCompleted.next(node);
    }

}
