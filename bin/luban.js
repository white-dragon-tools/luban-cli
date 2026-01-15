#!/usr/bin/env node
const { spawn } = require('child_process');
const path = require('path');

const dllPath = path.join(__dirname, '..', 'src', 'Luban', 'bin', 'Release', 'net8.0', 'Luban.dll');
const args = process.argv.slice(2);

const child = spawn('dotnet', [dllPath, ...args], {
  stdio: 'inherit',
  shell: process.platform === 'win32'
});

child.on('close', (code) => {
  process.exit(code);
});
