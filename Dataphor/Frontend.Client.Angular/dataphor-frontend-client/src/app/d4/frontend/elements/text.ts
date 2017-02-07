import { Input, OnInit, OnDestroy, ViewChildren } from '@angular/core';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';

import { IText } from '../interfaces';
import { InterfaceService } from '../interface';
import { NodeService } from '../nodes/index';
import { AlignedElement } from './index';

export class Text extends AlignedElement implements OnInit, IText {
    
    @Input('height') Height: number = 1;

    constructor() {
        super();
    }

};