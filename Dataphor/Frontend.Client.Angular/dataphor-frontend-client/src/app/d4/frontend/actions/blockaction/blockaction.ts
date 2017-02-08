import { Action } from '../';
import { IBlockable, INode, IAction } from '../../';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';

export class BlockAction extends Action implements IBlockable {

    private _completed$ = new BehaviorSubject<INode>(null);
    private _completedObserver = this._completed$.subscribe({
        next: (x) => {
            console.log('BlockableAction completed -- changed to ${ x }');
            this.DetatchBlockable(x as IBlockable);
        }
    });
    // DoCompleted
    protected Complete(node: INode): void {
        this._completed$.complete();
    }


    private _disposed$ = new BehaviorSubject<INode>(null);
    private _disposedObserver = this._disposed$.subscribe({
        next: (x) => {
            console.log('BlockableAction disposed');
            this.DetachBlockable(x as IBlockable);
        }
    });

    

    protected BlockableExecute(action: IAction, target: IAction): void {
        let blockable: IBlockable = action as IBlockable;
        if (blockable !== null) {
            blockable.OnCompletedObserver = this._completed$.subscribe(x => { });
        }
        action.Execute(this, target);

        if (blockable === null) {
            this.Complete(target);
        }
    }

    //protected DetachBlockable(blockable IBlockable)

}