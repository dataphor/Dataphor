import { Input, OnInit, OnDestroy, ViewChildren } from '@angular/core';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';

import { IImage, IImageSource, StretchStyles } from '../interfaces';
import { InterfaceService } from '../interface';
import { NodeService } from '../nodes/index';
import { TitledElement } from './index';

const DefaultImageHeight: number = 90;
const DefaultImageWidth: number = 90;

export class Image extends TitledElement implements OnInit, IImage {
    
    @Input('width') Width: number = 0;
    // If set to -1, use width of actual image
    @Input('imagewidth') ImageWidth: number = -1;
    // If set to -1, use width of actual image
    @Input('imageheight') ImageHeight: number = -1;
    @Input('stretchstyle') StretchStyle: StretchStyles = StretchStyles.StretchRatio;
    @Input('center') Center: boolean = true;

    // TODO: Figure out ImageCaptureForm equivalent

    @Input('imagesource') ImageSource: IImageSource;


    constructor() {
        super();
    }

    ngOnInit() {
        
    }
    
};