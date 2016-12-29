export class DisposableList<T> {
    constructor(itemsOwned?: boolean, capacity?: number) {
        if (itemsOwned) this._itemsOwned = itemsOwned;
        else this._itemsOwned = true;
    }
    

    protected _itemsOwned: boolean;
    get ItemsOwned(): boolean { return this._itemsOwned; }
    set ItemsOwned(value: boolean) { this._itemsOwned = value; }

    protected _disposed;
}