/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { LockPasswordComponent } from './lock-password.component';

describe('LockPasswordComponent', () => {
  let component: LockPasswordComponent;
  let fixture: ComponentFixture<LockPasswordComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [LockPasswordComponent],
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(LockPasswordComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
