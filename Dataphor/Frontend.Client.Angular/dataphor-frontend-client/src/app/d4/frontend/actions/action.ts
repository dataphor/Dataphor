import { Component, Input, OnInit, OnDestroy, ViewChildren } from '@angular/core';
import { Node, NodeService } from '../';
import { INode, IAction, ILayoutDisableable, IBlockable } from '../interfaces';
import { KeyedCollection } from '../../system';
import { Observable } from 'rxjs/Observable';
import { Subscription } from 'rxjs/Subscription';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { InterfaceService } from '../interface';

export enum NotifyIcon {
    None,
    Info,
    Warning,
    Error
}

export class Action extends Node implements IAction, OnInit, OnDestroy {

    constructor(private _interfaceService: InterfaceService, private _nodeService: NodeService) {
        super(_interfaceService, _nodeService);
        this.CheckExternals();
    }

    ngOnInit() {
        if (this.__afterExecute || this.__beforeExecute) {
            this._actionDictionaryObserver = this._interfaceService.ActionService.GetActionDictionarySubject().subscribe({
                next: (x) => {
                    this.HandleActionDictionaryChange();
                }
            });
        }
        if (this.__beforeExecute) {
            this._beforeExecute = this._interfaceService.ActionService.GetActionByName(this.__beforeExecute);
        }
        if (this.__afterExecute) {
            this._afterExecute = this._interfaceService.ActionService.GetActionByName(this.__afterExecute);
        }
        
    }

    @Input('text') __text: string;

    get Text(): string {
        return this.__text;
    }
    set Text(value: string) {
        if (this.__text !== value) {
            this.__text = value;
            this.TextChanged();
        }
    }

    @Input('hint') __hint: string;
    @Input('image') __image: string;
    get Image(): string {
        return this.__image;
    }
    set Image(value: string) {
        if (this.__image !== value) {
            this.__image = value;
            if (this.Active) {
                this.InternalUpdateImage();
            }
        }
    }

    @Input('beforeexecute') __beforeExecute: string;
    private _beforeExecute: IAction;
    get BeforeExecute(): IAction {
        return this._beforeExecute;
    }
    set BeforeExecute(value: IAction) {
        if (this._beforeExecute !== value) {
            //if (this._beforeExecute !== null) {
            //    this._beforeExecute.Disposed$.unsubscribe();
            //}
            this._beforeExecute = value;
            if (this._beforeExecute !== null) {
                this._beforeExecute.Disposed$ = new BehaviorSubject<Object>(null);
            }

        }
    }
    private _beforeExecuteLoaded: boolean = this.BeforeExecute ? false : true;
    
    @Input('afterexecute') __afterExecute: string;
    private _afterExecute: IAction;

    private _afterExecuteLoaded: boolean = this.AfterExecute ? false : true;

    @Input('visible') __visible: boolean;
    @Input('enabled') __enabled: boolean;
    get Enabled(): boolean {
        return this.__enabled;
    }
    set Enabled(value: boolean) {
        if (this.__enabled !== value) {
            this.__enabled = value;  
        }
    }

    // TODO: Figure out what to do with this 'User-Defined Scratchpad'
    @Input('userdata') UserData: Object;

    private _externalsLoaded: boolean = false;
    private _actionDictionaryObserver: Subscription;

    HandleActionDictionaryChange(): void {
        console.log('Action Dictionary Changed');
        if (this.AfterExecute) {
            this._afterExecute = this._interfaceService.ActionService.GetActionByName(this.AfterExecute);
        }
        if (this.BeforeExecute) {
            this._beforeExecute = this._interfaceService.ActionService.GetActionByName(this.BeforeExecute);
        }
        this.CheckExternals();
    }

    // check if referenced Actions have returned
    CheckExternals(): void {
        if (!this._afterExecuteLoaded) {
            if (this._afterExecute) {
                this._afterExecuteLoaded = true;
            }
        }
        if (!this._beforeExecuteLoaded) {
            if (this._beforeExecute) {
                this._beforeExecuteLoaded = true;
            }
        }
        if (this._afterExecuteLoaded && this._beforeExecuteLoaded) {
            this._externalsLoaded = true;
        }
    }


    Dispose(disposing: boolean): void {
        this._actionDictionaryObserver.unsubscribe();

        this.AfterExecute.Unsubscribe();
        this.BeforeExecute.Unsubscribe();
        super.Dispose(disposing);
    }

    Execute(sender?: INode, target?: INode): void {
        if (sender && target) {
            if (this.GetEnabled()) {
                if (this.DoBeforeExecute(sender, target)) {
                    this.FinishExecute(sender, target);
                }
            }
        }
    }

    // Override
    InternalExecute(sender: INode, target: INode) { }
    
    private BeforeExecuteDisposed(sender: INode, target: INode): void {
        this.BeforeExecute = null;
    }

    private DoBeforeExecute(sender: INode, target: INode): boolean {
        if (this._beforeExecute !== null) {
            let blockable: IBlockable = this._beforeExecute as IBlockable;
            if (blockable !== null) {
                blockable.OnCompleted$ = new BehaviorSubject<INode>(null);

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

    get AfterExecute(): IAction {
        return this._afterExecute;
    }
    set AfterExecute(value: IAction) {
        if (this._afterExecute !== value) {
            if (this._afterExecute !== null) {
                this._afterExecute.Disposed$.unsubscribe();
            }
            this._afterExecute = value;
            this._afterExecute.Disposed$ = new BehaviorSubject<Object>(null);
        }
    }

    private DoAfterExecute(target: INode): void {
        if (this._afterExecute !== null) {
            this._afterExecute.Execute(this, target);
        }
    }

    private _textChanged$ = new BehaviorSubject<string>(null);

    get OnTextChanged$() {
        return this._textChanged$;
    }

    private _textChangedObserver = this._textChanged$.subscribe({ next: (x) => { console.log('Text changed to ${ x }') }});
    

    GetText(): string {
        return this.__text;
    }

    GetDescription(): string {
        return this.__text.replace('.', '').replace('&', '');
    }

    protected TextChanged(): void {
        this._textChanged$.next(this.__text);
    }

    private _actualEnabled: boolean;

    private _enabledChanged$ = new BehaviorSubject<boolean>(null);
    private _enabledChangedObserver = this._enabledChanged$.subscribe({ next: (x) => { console.log('Enabled changed to ${ x }') } });
    EnabledChanged(): void {
        let enabled: boolean = this.GetEnabled();
        if (this._actualEnabled !== enabled) {
            this._actualEnabled = enabled;
            this._enabledChanged$.next(this._actualEnabled);
        }
        
    }

    GetEnabled(): boolean {
        return this.__enabled;
    }

    private _hintChanged$ = new BehaviorSubject<string>(null);
    private _hintChangedObserver = this._hintChanged$.subscribe({ next: (x) => { console.log('Hint changed to ${ x }') } });

    private _hint: string = '';

    get Hint(): string {
        return this._hint;
    }
    set Hint(value: string) {
        if (this._hint !== value) {
            this._hint == value;
            this._hintChanged$.next(this._hint);
        }
    }

    private _visibleChanged$ = new BehaviorSubject<boolean>(null);
    private _visibleChangedObserver = this._visibleChanged$.subscribe({ next: (x) => { console.log('Visible changed to ${ x }') } });

    private _visible: boolean = true;

    get Visible(): boolean {
        return this._visible;
    }
    set Visible(value: boolean) {
        if (this._visible !== value) {
            this._visible == value;
            this._visibleChanged$.next(this._visible);
        }
    }

    protected Activate(): void {
        super.Activate();
        this._actualEnabled = this.GetEnabled();
    }

    protected Deactivate(): void {
        try {
            super.Deactivate();
        } finally {
            this.SetImage(null);
        }
    }

    protected BeforeDeactivate(): void {
        super.BeforeDeactivate();
        this.CancelImageRequest();
    }

    protected AfterActivate(): void {
        this.InternalUpdateImage();
        super.AfterActivate();
    }

    private _imageChanged$ = new BehaviorSubject<Image>(null);
    private _imageChangedObserver = this._imageChanged$.subscribe({ next: (x) => { console.log('Hint changed to ${ x }') } });
    


    // TODO: Create means of taking image string and getting/displaying the image;
    private _loadedImage: Image;

    get LoadedImage(): Image {
        return this._loadedImage;
    }

    SetImage(value: Image): void {
        if (this._loadedImage !== null) {
            this._loadedImage.Dispose();
        }
        this._loadedImage = value;
        this._imageChanged$.next(this.value);
    }

    // TODO: Translate to RESTful request with Observable pattern
  //  private PipeRequest _imageRequest;

  //  private void CancelImageRequest()
		//{
  //  if (_imageRequest != null) {
  //      HostNode.Pipe.CancelRequest(_imageRequest);
  //      _imageRequest = null;
  //  }

    private InternalUpdateImage(): void {
        if (this.HostNode.Session.AreImagesLoaded()) {
            // TODO: unsubscribe from RESTful callback
            //this.CancelImageRequest();
            if (this.Image === '') {
                this.SetImage(null);
            } else {
                // TODO: Translate to RESTFul request with Observable pattern
                // Queue up an asynchronous request
                //_imageRequest = new PipeRequest(Image, new PipeResponseHandler(ImageRead), new PipeErrorHandler(ImageError));
                //HostNode.Pipe.QueueRequest(_imageRequest);
            }
        } else {
            this.SetImage(null);
        }
    }

    // TODO: Create API call--simple GetImage([body] params[]) sort of thing
    //private ImageRead(request: PipeRequest, pipe: Pipe): void {}
    //private ImageError(request: PipeRequest, pipe: Pipe, exception: Exception): void {}
    
    ngOnDestroy() {
        if (this._actionDictionaryObserver) {
            this._actionDictionaryObserver.unsubscribe();
        }
        this._textChangedObserver.unsubscribe();
        this._enabledChangedObserver.unsubscribe();
        this._hintChangedObserver.unsubscribe();
        this._visibleChangedObserver.unsubscribe();
        this._imageChangedObserver.unsubscribe();
    }

}

