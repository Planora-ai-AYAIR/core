export interface Environment {
  id: string;
  name: string;
  type: 'photosphere' | 'model';
  format?: 'hdr' | 'exr' | 'jpg' | 'png';
  path: string;
  thumbnail: string;
}
