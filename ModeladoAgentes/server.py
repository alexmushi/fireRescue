from http.server import BaseHTTPRequestHandler, HTTPServer
import logging
import json

from model import FireRescueModel
from util import serialize_doors

model = FireRescueModel()

model.print_map(model.walls.T, model.fires.data.T)

class Server(BaseHTTPRequestHandler):
    
    def _set_response(self):
        self.send_response(200)
        self.send_header('Content-Type', 'application/json')
        self.end_headers()
        
    def do_GET(self):
        self._set_response()
        self.wfile.write("GET request for {}".format(self.path).encode('utf-8'))

    def do_POST(self):

        if model.firstStep == True:
            data = {
                "damage_points": model.damage_points,
                "people_lost": model.people_lost,
                "people_rescued": model.people_rescued,
                "width": model.width,
                "height": model.height,
                "walls": model.walls.tolist(),
                "fires": model.fires.data.tolist(),
                "points_of_interest": model.points_of_interest.data.T.tolist(),
                "doors": serialize_doors(model.doors),
                "entry_points": model.entry_points
            }
            model.firstStep = False
        else:
            model.explosion((2,3))
            model.explosion((2,3))
            model.explosion((2,3))
            data = {
                "damage_points": model.damage_points,
                "people_lost": model.people_lost,
                "people_rescued": model.people_rescued,
                "width": model.width,
                "height": model.height,
                "walls": model.changes["walls"],
                "fires": model.changes["fires"],
                "damage": model.changes["damage"],
                "points_of_interest": model.changes["points_of_interest"],
                "doors": model.changes["doors"], 
                "explosions": model.changes["explosions"],
                "simulation_finished": model.simulationFinished
            }
            print(model.changes)
            model.print_map(model.walls.T, model.fires.data.T)

        json_data = json.dumps(data)

        self._set_response()
        self.wfile.write(json_data.encode('utf-8'))


def run(server_class=HTTPServer, handler_class=Server, port=8585):
    logging.basicConfig(level=logging.INFO)
    server_address = ('', port)
    httpd = server_class(server_address, handler_class)
    logging.info("Starting httpd...\n") # HTTPD is HTTP Daemon!
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:   # CTRL+C stops the server
        pass
    httpd.server_close()
    logging.info("Stopping httpd...\n")

if __name__ == '__main__':
    from sys import argv
    
    if len(argv) == 2:
        run(port=int(argv[1]))
    else:
        run()