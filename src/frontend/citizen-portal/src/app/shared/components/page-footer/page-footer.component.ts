import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-page-footer',
  templateUrl: './page-footer.component.html',
  styleUrls: ['./page-footer.component.scss'],
})
export class PageFooterComponent implements OnInit {
  @Input() public hasSecondaryAction: boolean;
  @Input() public disableSave: boolean;
  @Output() public save: EventEmitter<void>;
  @Output() public back: EventEmitter<void>;

  @Input() public saveButtonLabel: string;
  @Input() public secondaryActionButtonLabel: string;

  constructor() {
    this.hasSecondaryAction = true;

    this.save = new EventEmitter<void>();
    this.back = new EventEmitter<void>();
    this.saveButtonLabel = 'Next';
    this.secondaryActionButtonLabel = 'Back';
  }

  public ngOnInit(): void {
    //
  }

  public onSave(): void {
    this.save.emit();
  }

  public onSecondaryAction(): void {
    this.back.emit();
  }
}
