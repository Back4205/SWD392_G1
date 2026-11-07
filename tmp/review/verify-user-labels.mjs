import assert from 'node:assert/strict';
import { displayUserName, roleLabel, rolePrefix } from '../../SWD392_G1/frontend/src/features/auth/auth.types.ts';
for (const [key, label] of [
  ['Admin', 'Quản trị viên'], ['Manager', 'Quản lý kho'],
  ['Purchasing Manager', 'Quản lý mua hàng'], ['Warehouse Staff', 'Nhân viên kho'],
  ['Sales Staff', 'Nhân viên bán hàng']
]) assert.equal(roleLabel(key), label);
assert.equal(displayUserName({ fullName: 'Quản lý kho (Nguyễn Văn Quản)', userName: 'manager' }), 'Quản lý kho (Nguyễn Văn Quản)');
assert.equal(displayUserName({ fullName: 'Qu?n lý kho (Nguy?n Văn Qu?n)', userName: 'manager' }), 'manager');
assert.equal(displayUserName({ fullName: 'Nhân viên �', userName: 'staff' }), 'staff');
assert.equal(displayUserName({ fullName: ' ', userName: 'admin' }), 'admin');
assert.equal(rolePrefix('Manager'), '/manager');
assert.equal(rolePrefix('Warehouse Staff'), '/staff');
console.log('11 Vietnamese label/name/routing checks passed');
