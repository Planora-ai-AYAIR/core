/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { MitigationSuggestionsComponent } from './mitigation-suggestions.component';

describe('MitigationSuggestionsComponent', () => {
  let component: MitigationSuggestionsComponent;
  let fixture: ComponentFixture<MitigationSuggestionsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ MitigationSuggestionsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MitigationSuggestionsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
