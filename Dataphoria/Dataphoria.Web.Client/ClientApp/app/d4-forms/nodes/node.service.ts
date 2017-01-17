import { Node } from '../node';
import { INode, IHost } from '../interfaces/index';
import { KeyedCollection } from '../system';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { Injectable, OnDestroy } from '@angular/core';

@Injectable()
export class NodeService implements OnDestroy {

    private _nodeDictionary: KeyedCollection<INode>;
    private _nodeDictionarySubject$: BehaviorSubject<KeyedCollection<INode>>;

    constructor() {
        this._nodeDictionary = new KeyedCollection<INode>();
        this._nodeDictionarySubject$ = new BehaviorSubject<KeyedCollection<INode>>(null);
    }


    GetNodeByName(nodeName: string): INode {
        return this._nodeDictionary.Item(nodeName);
    }

    GetAllNodes(): KeyedCollection<INode> {
        return this._nodeDictionary;
    }

    AddNode(node: INode): void {
        this._nodeDictionary.Add(node.Name, node);
        this.NotifySubscribers();
    }

    RemoveNode(node: INode): void {
        this._nodeDictionary.Remove(node.Name);
        this.NotifySubscribers();
    }

    // Returns an Array of children to the current Node
    GetChildren(node: INode): Array<INode> {
        let children: Array<INode>;
        for (let child of node.Children) {
            if (node.Name == this.GetParent(child).Name) {
                children.push(child);
            }
        }
        return children;
    }

    // Removes a child from the Array of children of a given Node
    DisownChild(sourceNode: INode, childNode: INode): void {
        let childIndex = sourceNode.Children.indexOf(childNode);
        if (childIndex !== -1) {
            sourceNode.Children.splice(childIndex, 1);
        }
    }

    DisownChildAt(sourceNode: INode, childIndex: number): void {
        try {
            sourceNode.Children.splice(childIndex, 1);
        } catch (exception) {
            console.log('Could not find index of child to disown\n' + exception);
        }
    }


    GetParent(node: INode): INode {
        let parent: INode;
        let keys = this._nodeDictionary.Keys();
        for (let key of keys) {
            let item = this._nodeDictionary.Item(key);
            for (let child of item.Children) {
                if (child.Name === node.Name) {
                    return child;
                }
            }
        }
        // not found (must be host node)
        return null;
    }

    // Returns the host (top parent) Node
    GetHost(node: INode): INode {
        let current: INode = node;
        while (current.Owner != null) {
            current = current.Owner;
        }
        return current as IHost;
    }

    GetNodeDictionarySubject(): BehaviorSubject<KeyedCollection<INode>> {
        return this._nodeDictionarySubject$;
    }

    NotifySubscribers(): void {
        this._nodeDictionarySubject$.next(this._nodeDictionary);
    }

    ngOnDestroy() {
        this._nodeDictionarySubject$.unsubscribe();
    }

}
