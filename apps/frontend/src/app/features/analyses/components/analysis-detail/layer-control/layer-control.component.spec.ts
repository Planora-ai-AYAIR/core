/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { LayerControlComponent } from './layer-control.component';

describe('LayerControlComponent', () => {
  let component: LayerControlComponent;
  let fixture: ComponentFixture<LayerControlComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ LayerControlComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(LayerControlComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
