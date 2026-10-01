import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CachedNode } from '../../models/cached-node';

@Component({
  selector: 'app-node-editor',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './node-editor.component.html',
  styleUrl: './node-editor.component.scss'
})
export class NodeEditorComponent implements OnChanges {
  @Input() node: CachedNode | undefined;
  @Output() valueChange = new EventEmitter<string>();
  @Output() addChild = new EventEmitter<string>();
  @Output() deleteNode = new EventEmitter<void>();

  readonly value = signal('');
  childValue = '';

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['node']) {
      this.value.set(this.node?.value ?? '');
    }
  }

  save(): void {
    if (!this.node || this.node.state === 'deleted') return;
    const value = this.value().trim();
    if (!value) return;
    this.valueChange.emit(value);
  }

  createChild(): void {
    if (!this.node || this.node.state === 'deleted') return;
    const value = this.childValue.trim();
    if (!value) return;
    this.addChild.emit(value);
    this.childValue = '';
  }
}
