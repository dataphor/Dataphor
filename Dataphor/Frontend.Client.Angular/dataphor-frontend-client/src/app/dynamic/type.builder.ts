import { Component, ComponentFactory, NgModule, Input, Injectable} from '@angular/core';
import { JitCompiler } from '@angular/compiler';
import { D4FormsModule } from '../d4-forms/d4-forms.module';

import * as _ from 'lodash';

export interface IHaveDynamicData {
    entity: any;
}

@Injectable()
export class DynamicTypeBuilder {

    private _cacheOfFactories: { [templateKey: string]: ComponentFactory<IHaveDynamicData> } = {};

    constructor(protected compiler: JitCompiler) {}

    public createComponentFactory(template: string): Promise<ComponentFactory<IHaveDynamicData>> {

        let factory = this._cacheOfFactories[template];

        if (factory) {
            console.log('Module and Type were returned from the cache');
            return new Promise((resolve) => {
                resolve(factory);
            });
        }

        // unknown template ... let's create a Type for it
        const type = this.createNewComponent(template);
        const module = this.createComponentModule(type);

        return new Promise((resolve) => {
            this.compiler
                .compileModuleAndAllComponentsAsync(module)
                .then((moduleWithFactories) => {
                    factory = _.find(moduleWithFactories.componentFactories, { componentType: type });
                    this._cacheOfFactories[template] = factory;
                    resolve(factory);
            });
        });
    };

    protected createNewComponent (tmpl: string) {

        @Component({
            selector: 'd4-dynamic-component',
            template: tmpl,
        })
        class CustomDynamicComponent implements IHaveDynamicData {
            @Input() public entity: any;
        };
        // a component for this particular template
        return CustomDynamicComponent;
    }

    protected createComponentModule (componentType: any) {
        @NgModule({
            imports: [ 
                //D4FormsModule // TODO: Fix before importing (breaks build to import when broken)
                ],
            declarations: [ componentType ],
        })
        class RuntimeComponentModule {}
        // a module for just this Type
        return RuntimeComponentModule;
    }
}
