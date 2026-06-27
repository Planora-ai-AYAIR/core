/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { BoreholeParamsComponent } from './borehole-params.component';

describe('BoreholeParamsComponent', () => {
  let component: BoreholeParamsComponent;
  let fixture: ComponentFixture<BoreholeParamsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ BoreholeParamsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(BoreholeParamsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
