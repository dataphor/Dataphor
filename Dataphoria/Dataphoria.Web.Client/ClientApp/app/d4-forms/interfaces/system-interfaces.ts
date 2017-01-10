// Catchall meant to represent all the nice C# interfaces we don't get in TS

import { BehaviorSubject } from 'rxjs/BehaviorSubject';

export interface IDisposable {
    Dispose(): void;
}

export interface IContainer extends IDisposable {
    Components: Array<IComponent>;
    Add(component: IComponent, name?: string): void;
    Remove(component: IComponent);
}

export interface IServiceProvider {
    GetService(serviceType: string): Object;
}

export interface ISite extends IServiceProvider {
    Component: IComponent;
    Container: IContainer;
    DesignMode: boolean;
    Name: string;
}

export interface IComponent extends IDisposable {
    Site: ISite;
    Disposed: BehaviorSubject<Object>
}

