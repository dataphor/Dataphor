import { Node } from './node';
import { INode, IAction, ILayoutDisableable, IBlockable } from './interfaces';
import { KeyedCollection } from './system';
import { Observable } from 'rxjs/Observable';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { OnDestroy } from '@angular/core';

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

export class Action extends Node implements IAction, OnDestroy {

    Dispose(disposing: boolean): void {
        this.AfterExecute.Unsubscribe();
        this.BeforeExecute.Unsubscribe();
        super.Dispose(disposing);
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
            if (this._beforeExecute !== null) {
                this._beforeExecute.Disposed$.unsubscribe();
            }
            this._beforeExecute = value;
            if (this._beforeExecute !== null) {
                this._beforeExecute.Disposed$ = new BehaviorSubject<Object>(null);
            }

        }
    }

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

    private _afterExecute: IAction;

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
    private _text: string = '';

    get Text(): string {
        return this._text;
    }
    set Text(value: string) {
        if (this._text !== value) {
            this._text = value;
            this.TextChanged();
        }
    }

    GetText(): string {
        return this._text;
    }

    GetDescription(): string {
        return this._text.replace('.', '').replace('&', '');
    }

    protected TextChanged(): void {
        this._textChanged$.next(this._text);
    }

    private _enabled: boolean = true;

    get Enabled(): boolean {
        return this._enabled;
    }
    set Enabled(value: boolean) {
        if (this._enabled !== value) {
            this._enabled = value;
            
        }
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
        return this._enabled;
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
    private _visibleChangedObserver = this._visibleChanged$.subscribe({ next: (x) => { console.log('Hint changed to ${ x }') } });

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
    private _image: string = '';
    get Image(): string {
        return this._image;
    }
    set Image(value: string) {
        if (this._image !== value) {
            this._image = value;
            if (this.Active) {
                this.InternalUpdateImage();
            }
        }
    }

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
        this._textChangedObserver.unsubscribe();
        this._enabledChangedObserver.unsubscribe();
        this._hintChangedObserver.unsubscribe();
        this._visibleChangedObserver.unsubscribe();
        this._imageChangedObserver.unsubscribe();
    }

}

export class BlockableAction extends Action implements IBlockable {

    private _onCompleted = new BehaviorSubject<INode>(null);

    OnCompleted$ = this._onCompleted.asObservable();

    protected DoCompleted(node: INode): void {
        this._onCompleted.next(node);
    }

}
