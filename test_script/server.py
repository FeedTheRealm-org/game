from http.server import HTTPServer, SimpleHTTPRequestHandler
import os

class TextureHandler(SimpleHTTPRequestHandler):
    def do_GET(self):
        # Remove leading slash from path
        texture_name = self.path[1:]
        
        # Add .png extension if not present
        if not texture_name.endswith('.png'):
            texture_name += '.png'
        
        # Look for the file in current directory
        if os.path.exists(texture_name):
            self.send_response(200)
            self.send_header('Content-type', 'image/png')
            self.send_header('Access-Control-Allow-Origin', '*')
            self.end_headers()
            
            with open(texture_name, 'rb') as f:
                self.wfile.write(f.read())
        else:
            self.send_response(404)
            self.end_headers()
            self.wfile.write(b'Texture not found')

if __name__ == '__main__':
    PORT = 8080
    server = HTTPServer(('localhost', PORT), TextureHandler)
    print(f'Server running on http://localhost:{PORT}')
    print('Put your texture files (hair_1.png, etc.) in the same folder as this script')
    server.serve_forever()
