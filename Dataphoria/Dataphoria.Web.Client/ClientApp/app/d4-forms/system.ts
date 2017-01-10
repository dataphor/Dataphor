import { EventEmitter } from '@angular/core';


export class ArrayUtility<T> {

    // Inserts an array into another array, starting at the given index
    // index: starting index for insert
    // source: source array
    // items: array to insert into source array
    static InsertRange<T>(index: number, source: Array<T>, items: Array<T>): Array<T> {
        return source.splice(index, 0, ...items);
    }
    static Remove<T>(value: T, source: Array<T>): Array<T> {
        try {
            let index: number = source.indexOf(value);
            source = source.splice(index, 1);
        } catch (exception) {
            // TODO: Log exception
        }
        return source;
    }
    static SafeRemove<T>(value: T, source: Array<T>): Array<T> {
        let index: number = source.indexOf(value);
        return index >= 0 ? source.splice(index, 1) : source; 
    }

    static RemoveAt<T>(index: number, collection: Array<T>): T {
        try {
            let item: T = collection[index];
            collection = collection.splice(index, 1);
            return item;
        } catch (exception) {
            // TODO: Log exception
        }
    }

}

//export class ValidatingBaseList<T> {
//    constructor() {

//    }

//    private _count: number;
//    get Count(): number { return this._count; }

//    private _items: Array<T>;

//    protected Validate(tempValue: T): void { }

//    protected Adding(tempValue: T, index: number): void { }

//    protected Removing(tempValue: T, index: number): void { }

//    protected Removed(tempValue: T, index: number): void { }

//    Add(tempValue: T): number {
//        let index: number = this.Count;
//        this.Insert(index, tempValue);
//        return index;
//    }

//    AddRange(items: Array<T>): void {
//        for (let item of items) {
//            this.Add(item);
//        }
//    }

//    Insert(index: number, tempValue: T): void {
//        this.Validate(tempValue);
//        for (let localIndex: number = this._count - 1; localIndex >= index; localIndex--) {
//            this._items[localIndex + 1] = this._items[localIndex];
//        }
//        this._items[index] = tempValue;
//        this._count++;
//        this.Adding(tempValue, index);
//    }

//    InsertRange(index: number, items: Array<T>): void {
//        for (let item of items) {
//            this.Insert(index++, item);
//        }
//    }

//    Remove(tempValue: T): void {
//        this.RemoveAt(this.IndexOf(tempValue));
//    }

//    SafeRemove(tempValue: T): void {
//        let index: number = this.IndexOf(tempValue);
//        if (index >= 0) {
//            this.RemoveAt(index);
//        }
//    }

//    RemoveAt(index: number): T {
//        let item: T = this._items[index];
//        try {
//            this.Removing(item, index);
//        } finally {
//            this._count--;
//            this._items = this._items.splice(index, 1);
//        }
//        this.Removed(item, index);
//        return item;
//    }

//    RemoveRange(index: number, count: number): void {
//        for (let localIndex: number = 0; localIndex < count; localIndex++) {
//            this.RemoveAt(index);
//        }
//    }

//    Clear(): void {
//        // Should this be _count instead? 
//        while (this.Count > 0) {
//            this.RemoveAt(this.Count - 1);
//        }
//    }

//    SetRange(index: number, items: Array<T>): void {
//        for (let item of items) {
//            this._items[index++] = item;
//        }
//    }

//    IndexOf(item: T) {
//        return this._items.indexOf(item);
//    }

//}



//export class DisposableList<T> extends ValidatingBaseList<T> {
//    constructor(itemsOwned?: boolean) {
//        super();
//        if (itemsOwned) {
//            this._itemsOwned = itemsOwned;
//        } else {
//            this._itemsOwned = true;
//        }
//    }

//    protected _itemsOwned: boolean;
//    get ItemsOwned(): boolean { return this._itemsOwned; }
//    set ItemsOwned(value: boolean) { this._itemsOwned = value; }

//    protected _disposed;

//    Disposed: EventEmitter<Object>;

//    Dispose(): void {
//        this._disposed = true;
//        if (this.Disposed !== null) {
//            this.Disposed.emit(this);
//        }

//        // let exception: Exception = null
//        while (super.Count > 0) {
//            try {
//                super.RemoveAt(0);
//            } catch (ex) {
//                // exception = ex
//            }
//        }

//        // if (exception !== null) {
//           // throw exception 
//        // }

//    }

//    protected ItemDispose(sender: Object, args: EventArgs) {
//        this.Disown(sender as T);
//    }

//    protected Adding(value: T, index: number): void {
//        if (value is IDisposableNotify) {
//            (value as IDisposableNotify).Disposed += new EventEmitter()
//        }
//    }

//}


// Dictionary Interface
export interface IKeyedCollection<T> {
    Add(key: string, value: T);
    ContainsKey(key: string): boolean;
    Count(): number;
    Item(key: string): T;
    Keys(): string[];
    Remove(key: string): T;
    Values(): T[];
}

// Dictionary Definition
export class KeyedCollection<T> implements IKeyedCollection<T> {
    private items: { [index: string]: T } = {};

    private count: number = 0;

    public ContainsKey(key: string): boolean {
        return this.items.hasOwnProperty(key);
    }

    public Count(): number {
        return this.count;
    }

    public Add(key: string, value: T) {
        this.items[key] = value;
        this.count++;
    }

    public Remove(key: string): T {
        var val = this.items[key];
        delete this.items[key];
        this.count--;
        return val;
    }

    public Item(key: string): T {
        return this.items[key];
    }

    public Keys(): string[] {
        return Object.keys(this.items);
    }

    public Values(): T[] {
        var values: T[] = [];

        for (var prop in this.items) {
            if (this.items.hasOwnProperty(prop)) {
                values.push(this.items[prop]);
            }
        }

        return values;
        // return Object.values(this.items); // Unfortunately, this has limited browser support at this point
    }
}
