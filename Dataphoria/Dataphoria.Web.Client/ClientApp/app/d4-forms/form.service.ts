import { Injectable } from '@angular/core';
import { ISource, IComponent } from './interfaces';
import { Source } from './models';
import { UtilityService, KeyedCollection } from './utility.service';

@Injectable()
export class FormService {

    SourceDictionary: KeyedCollection<ISource>;

    constructor() {
        this.SourceDictionary = new KeyedCollection<ISource>();
    }

    registerSource(source: ISource): ISource {
        if (this.SourceDictionary.ContainsKey(source.Name)) {
            return this.SourceDictionary.Item(source.Name);
        }
        this.SourceDictionary.Add(source.Name, new Source(source));
        return this.SourceDictionary.Item(source.Name);
    }

    updateSource(source: ISource): ISource
    {
        if (this.SourceDictionary.ContainsKey(source.Name)) {
            this.SourceDictionary.Remove(source.Name);
            return this.registerSource(source);
        }
        // Notify listeners? 
    }
    registerSourceListener(sourceName: string, listener: IComponent) {
        if (this.SourceDictionary.ContainsKey(sourceName)) {
            // TODO: Register Listeners (components) to the given source 
            //this.SourceDictionary.Item(sourceName).
        }
    }
    
}