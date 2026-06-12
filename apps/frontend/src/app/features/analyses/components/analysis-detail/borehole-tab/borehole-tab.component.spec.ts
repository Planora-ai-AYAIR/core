/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { BoreholeTabComponent } from './borehole-tab.component';

describe('BoreholeTabComponent', () => {
  let component: BoreholeTabComponent;
  let fixture: ComponentFixture<BoreholeTabComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ BoreholeTabComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(BoreholeTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
