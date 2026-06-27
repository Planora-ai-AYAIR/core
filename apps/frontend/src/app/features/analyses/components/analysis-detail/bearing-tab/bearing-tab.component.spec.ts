/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { BearingTabComponent } from './bearing-tab.component';

describe('BearingTabComponent', () => {
  let component: BearingTabComponent;
  let fixture: ComponentFixture<BearingTabComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ BearingTabComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(BearingTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
